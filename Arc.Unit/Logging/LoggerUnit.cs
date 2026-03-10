// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

public class LoggerScope : ILogContext
{
    private readonly LoggerResolverDelegate[] loggerResolvers;
    private readonly IServiceProvider serviceProvider;
    private readonly IConsoleService consoleService;

    private ConcurrentDictionary<LogSourceLevelPair, ILogWriter?> sourceLevelToLogger = new();

    public LoggerScope(UnitContext unitContext, IServiceProvider serviceProvider, IConsoleService consoleService)
    {
        this.loggerResolvers = unitContext.LoggerResolvers;
        this.serviceProvider = serviceProvider;
        this.consoleService = consoleService;
    }

    public IConsoleService ConsoleService => this.consoleService;

    public ILogWriter? TryGet<TLogSource>(LogLevel logLevel = LogLevel.Information)
    {
        return this.sourceLevelToLogger.GetOrAdd(new(typeof(TLogSource), logLevel), x =>
        {
            LoggerResolverContext context = new(x);
            for (var i = 0; i < this.loggerResolvers.Length; i++)
            {
                this.loggerResolvers[i](context);
            }

            if (context.LogOutputType is not null)
            {
                if (this.serviceProvider.GetService(context.LogOutputType) is ILogOutput logOutput)
                {
                    var logFilter = context.LogFilterType == null ? null : (ILogFilter)this.serviceProvider.GetRequiredService(context.LogFilterType);
                    return new LogInstance(this, x.LogSourceType, x.LogLevel, logOutput, logFilter);
                }
            }

            return null;
        });
    }
}

public class LoggerUnit
{
    internal static long OffsetTicks { get; private set; }

    public static void SetTimeOffset(TimeSpan timeSpan)
    {
        OffsetTicks = timeSpan.Ticks; // (long)(timeSpan.TotalSeconds * Stopwatch.Frequency);
    }

    public static void Configure(IUnitConfigurationContext context)
    {
        // Main
        context.TryAddSingleton<LoggerUnit>();
        context.TryAddScoped<LoggerScope>();
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

    private IServiceProvider serviceProvider;
    private ConcurrentDictionary<BufferedLogOutput, BufferedLogOutput> logsToFlush = new();

    #endregion

    public LoggerUnit(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public ILogger<TLogSource> GetLogger<TLogSource>()
        => this.serviceProvider.GetRequiredService<ILogger<TLogSource>>();

    public ILogger GetLogger(Type logSource)
        => (ILogger)this.serviceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(logSource));

    public bool TryRegisterFlush(BufferedLogOutput logFlush)
        => this.logsToFlush.TryAdd(logFlush, logFlush);

    public async Task Flush()
    {
        var logs = this.logsToFlush.Keys.ToArray();
        foreach (var x in logs)
        {
            await x.Flush(false).ConfigureAwait(false);
        }
    }

    public async Task FlushConsole()
    {
        var logs = this.logsToFlush.Keys.Where(x => x.GetType() == typeof(ConsoleLogger)).ToArray();
        foreach (var x in logs)
        {
            await x.Flush(false).ConfigureAwait(false);
        }
    }

    public async Task FlushAndTerminate()
    {
        var logs = this.logsToFlush.Keys.ToArray();
        foreach (var x in logs)
        {
            await x.Flush(true).ConfigureAwait(false);
        }
    }
}
