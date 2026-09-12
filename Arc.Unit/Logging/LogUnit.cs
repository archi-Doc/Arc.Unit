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
    public const bool IsWorkerGroupIndependent = true;

    /// <summary>
    /// The name of the execution group which owns the log workers.
    /// </summary>
    public const string WorkerGroupName = "Logger";

    /// <summary>
    /// Gets the global log timestamp offset in ticks.
    /// </summary>
    internal static long OffsetTicks { get; private set; }

    /// <summary>
    /// Sets a global timestamp offset applied by the logging pipeline.
    /// </summary>
    /// <param name="offset">The offset to apply to log time values.</param>
    public static void SetTimestampOffset(TimeSpan offset)
    {
        OffsetTicks = offset.Ticks;
    }

    /// <summary>
    /// Registers core logging services, log outputs, options, and the default resolver.
    /// </summary>
    /// <param name="context">The unit configuration context used to register services.</param>
    public static void Configure(IUnitConfigurationContext context)
    {
        // Main
        context.AddSingleton<LogUnit>();
        context.AddScoped<ILogService, LogService>();

        // ILogger
        context.Services.Add(ServiceDescriptor.Scoped(typeof(ILogger), typeof(LoggerFactory<DefaultLogSource>)));
        context.Services.Add(ServiceDescriptor.Scoped(typeof(ILogger<>), typeof(LoggerFactory<>)));

        // Empty log output
        context.TryAddSingleton<EmptyLogOutput>();

        // Memory log output
        context.TryAddSingleton<MemoryLogOutput>();
        context.TryAddSingleton<MemoryLogOutputOptions>();

        // Console log output
        context.TryAddSingleton<ConsoleLogOutput>();
        context.TryAddSingleton<ConsoleLogOutputOptions>();

        // File log output
        context.Services.Add(ServiceDescriptor.Singleton(typeof(FileLogOutput<>), typeof(FileLogOutputFactory<>)));
        context.TryAddSingleton<FileLogOutputOptions>();

        // Console and file log output
        context.TryAddSingleton<ConsoleAndFileLogOutput>();

        // Default resolver
        context.AddLogOutputResolver(x =>
        {
            x.SetOutput<ConsoleLogOutput>();
        });
    }

    #region FieldAndProperty

    private readonly IServiceProvider serviceProvider;
    private readonly LogOutputResolver[] logOutputResolvers;
    private readonly ConcurrentDictionary<LogSourceLevelPair, LogBroker?> brokers = new();
    private readonly Lock flushTargetsLock = new();
    private BufferedLogOutput[] flushTargets = [];

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="LogUnit"/> class.
    /// </summary>
    /// <param name="unitContext">The runtime unit context containing services and resolvers.</param>
    public LogUnit(UnitContext unitContext)
    {
        this.serviceProvider = unitContext.ServiceProvider;
        this.logOutputResolvers = unitContext.LogOutputResolvers;
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
    {
        ArgumentNullException.ThrowIfNull(logOutput);
        lock (this.flushTargetsLock)
        {
            foreach (var target in this.flushTargets)
            {
                if (ReferenceEquals(target, logOutput))
                {
                    return false;
                }
            }

            Volatile.Write(ref this.flushTargets, [.. this.flushTargets, logOutput]);
            return true;
        }
    }

    /// <summary>
    /// Flushes all registered buffered outputs without termination.
    /// </summary>
    /// <returns>A task that represents the asynchronous flush operation.</returns>
    public Task FlushAsync() => this.FlushTargets(false, false);

    /// <summary>
    /// Flushes only registered console outputs without termination.
    /// </summary>
    /// <returns>A task that represents the asynchronous flush operation.</returns>
    public Task FlushConsoleAsync() => this.FlushTargets(false, true);

    /// <summary>
    /// Flushes all registered buffered outputs and requests termination semantics.
    /// </summary>
    /// <returns>A task that represents the asynchronous flush and termination operation.</returns>
    public Task FlushAndTerminateAsync() => this.FlushTargets(true, false);

    internal static ExecutionGroup GetGroup(ExecutionRoot root)
        => root.IndependentGroup.GetOrAddGroup(IsWorkerGroupIndependent, WorkerGroupName);

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

    private Task FlushTargets(bool terminate, bool consoleOnly)
    {
        var targets = Volatile.Read(ref this.flushTargets);
        Task? first = null;
        List<Task>? pending = null;
        foreach (var target in targets)
        {
            if (consoleOnly && target is not ConsoleLogOutput)
            {
                continue;
            }

            Task task;
            try
            {
                task = target.FlushAsync(terminate);
            }
            catch (Exception exception)
            {
                task = Task.FromException(exception);
            }

            if (task.IsCompletedSuccessfully)
            {
                continue;
            }

            if (first is null)
            {
                first = task;
            }
            else
            {
                pending ??= [first];
                pending.Add(task);
            }
        }

        return pending is not null ? Task.WhenAll(pending) : first ?? Task.CompletedTask;
    }

    private LogBroker? ResolveLogBroker(LogSourceLevelPair pair)
    {
        var context = new LogOutputResolverContext(pair);
        var resolvers = this.logOutputResolvers;
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
