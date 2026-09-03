// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Arc.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Central logging composition root and runtime broker cache for the unit system.
/// </summary>
public class LogUnit
{
    /// <summary>
    /// A value indicating whether the execution group of the log workers is independent of the parent (so that logs are written during termination).
    /// </summary>
    public const bool IsGroupIndependent = true;

    /// <summary>
    /// The name of the execution group which owns the log workers.
    /// </summary>
    public const string GroupName = "Logger";

    /// <summary>
    /// Gets the global log timestamp offset in ticks.
    /// </summary>
    internal static long OffsetTicks { get; private set; }

    /// <summary>
    /// Sets a global timestamp offset applied by the logging pipeline.
    /// </summary>
    /// <param name="timeSpan">The offset to apply to log time values.</param>
    public static void SetTimeOffset(TimeSpan timeSpan)
    {
        OffsetTicks = timeSpan.Ticks;
    }

    /// <summary>
    /// Registers core logging services, logger outputs, options, and the default resolver.
    /// </summary>
    /// <param name="context">The unit configuration context used to register services.</param>
    public static void Configure(IUnitConfigurationContext context)
    {
        // Main
        context.AddSingleton<LogUnit>();
        context.AddScoped<ILogService, LogService>();

        // ILogger
        context.Services.Add(ServiceDescriptor.Scoped(typeof(ILogger), typeof(LoggerFactory<DefaultLog>)));
        context.Services.Add(ServiceDescriptor.Scoped(typeof(ILogger<>), typeof(LoggerFactory<>)));

        // Empty logger
        context.TryAddSingleton<EmptyLogger>();

        // Memory logger
        context.TryAddSingleton<MemoryLogger>();
        context.TryAddSingleton<MemoryLoggerOptions>();

        // Console logger
        context.TryAddSingleton<ConsoleLogger>();
        context.TryAddSingleton<ConsoleLoggerOptions>();

        // File logger
        context.Services.Add(ServiceDescriptor.Singleton(typeof(FileLogger<>), typeof(FileLoggerFactory<>)));
        context.TryAddSingleton<FileLoggerOptions>();

        // Console and file logger
        context.TryAddSingleton<ConsoleAndFileLogger>();

        // Default resolver
        context.AddLoggerResolver(x =>
        {
            x.SetOutput<ConsoleLogger>();
        });
    }

    #region FieldAndProperty

    private readonly IServiceProvider serviceProvider;
    private readonly LoggerResolverDelegate[] loggerResolvers;
    private readonly ConcurrentDictionary<LogSourceLevelPair, LogBroker?> brokers = new();
    private readonly ConcurrentDictionary<BufferedLogOutput, BufferedLogOutput> logOutputsToBeFlushed = new();

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="LogUnit"/> class.
    /// </summary>
    /// <param name="unitContext">The runtime unit context containing services and resolvers.</param>
    public LogUnit(UnitContext unitContext)
    {
        this.serviceProvider = unitContext.ServiceProvider;
        this.loggerResolvers = unitContext.LoggerResolvers;
    }

    /// <summary>
    /// Gets the root <see cref="ILogService"/> from dependency injection.
    /// </summary>
    public ILogService RootLogService => field ??= this.serviceProvider.GetRequiredService<ILogService>();

    /// <summary>
    /// Registers a buffered output so it participates in future flush operations.
    /// </summary>
    /// <param name="logOutput">The buffered log output instance.</param>
    /// <returns>
    /// <see langword="true"/> if the output was newly registered; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This is called by the constructor of <see cref="BufferedLogOutput"/>, so it is usually not necessary to call it directly.
    /// </remarks>
    public bool RegisterFlushTarget(BufferedLogOutput logOutput)
        => this.logOutputsToBeFlushed.TryAdd(logOutput, logOutput);

    /// <summary>
    /// Flushes all registered buffered outputs without termination.
    /// </summary>
    /// <returns>A task that represents the asynchronous flush operation.</returns>
    public async Task Flush()
    {
        var flushTasks = this.logOutputsToBeFlushed.Keys.Select(x => x.Flush(false));
        await Task.WhenAll(flushTasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Flushes only registered console outputs without termination.
    /// </summary>
    /// <returns>A task that represents the asynchronous flush operation.</returns>
    public async Task FlushConsole()
    {
        var flushTasks = this.logOutputsToBeFlushed.Keys
            .Where(x => x is ConsoleLogger)
            .Select(x => x.Flush(false));
        await Task.WhenAll(flushTasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Flushes all registered buffered outputs and requests termination semantics.
    /// </summary>
    /// <returns>A task that represents the asynchronous flush and termination operation.</returns>
    public async Task FlushAndTerminate()
    {
        var flushTasks = this.logOutputsToBeFlushed.Keys.Select(x => x.Flush(true));
        await Task.WhenAll(flushTasks).ConfigureAwait(false);
    }

    internal static ExecutionGroup GetGroup(ExecutionRoot root)
        => root.IndependentGroup.GetOrAddGroup(IsGroupIndependent, GroupName);

    /// <summary>
    /// Gets or creates a cached <see cref="LogBroker"/> for the specified source type and level.
    /// </summary>
    /// <typeparam name="TLogSource">The log source marker type.</typeparam>
    /// <param name="logLevel">The minimum level represented by the broker key.</param>
    /// <returns>
    /// A resolved <see cref="LogBroker"/> when an output can be resolved; otherwise <see langword="null"/>.
    /// </returns>
    internal LogBroker? GetLogBroker<TLogSource>(LogLevel logLevel)
        => this.GetLogBroker(typeof(TLogSource), logLevel);

    /// <summary>
    /// Gets or creates a cached <see cref="LogBroker"/> for the specified source type and level.
    /// </summary>
    /// <param name="logSourceType">The log source marker type.</param>
    /// <param name="logLevel">The minimum level represented by the broker key.</param>
    /// <returns>
    /// A resolved <see cref="LogBroker"/> when an output can be resolved; otherwise <see langword="null"/>.
    /// </returns>
    internal LogBroker? GetLogBroker(Type logSourceType, LogLevel logLevel)
        => this.brokers.GetOrAdd(
            new(logSourceType, logLevel),
            static (pair, logUnit) => logUnit.ResolveLogBroker(pair), // Static lambda: no closure is allocated.
            this);

    private LogBroker? ResolveLogBroker(LogSourceLevelPair pair)
    {
        var context = new LoggerResolverContext(pair);
        var resolvers = this.loggerResolvers;
        for (var i = 0; i < resolvers.Length; i++)
        {
            resolvers[i](context);
        }

        if (context.LogOutputType is not null &&
            this.serviceProvider.GetService(context.LogOutputType) is ILogOutput logOutput)
        {
            var logFilter = context.LogFilterType is null ?
                null : (ILogFilter)this.serviceProvider.GetRequiredService(context.LogFilterType);
            return new LogBroker(pair.LogSourceType, pair.LogLevel, logOutput, logFilter);
        }

        return default;
    }
}
