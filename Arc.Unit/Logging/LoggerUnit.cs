// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

public class LoggerUnit : ILogContext
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
        context.Services.Add(ServiceDescriptor.Singleton<ILogWriter>(x => x.GetService<LoggerUnit>()?.Get<DefaultLog>() ?? throw new LoggerNotFoundException(typeof(DefaultLog), LogLevel.Information)));
        context.Services.Add(ServiceDescriptor.Singleton(typeof(ILogger), typeof(LoggerFactory<DefaultLog>)));
        context.Services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(LoggerFactory<>)));

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

    private readonly IConsoleService consoleService;
    private readonly LoggerResolverDelegate[] loggerResolvers;

    private IServiceProvider serviceProvider;
    private ConcurrentDictionary<LogSourceLevelPair, ILogWriter?> sourceLevelToLogger = new();
    private ConcurrentDictionary<BufferedLogOutput, BufferedLogOutput> logsToFlush = new();

    public IConsoleService ConsoleService => this.consoleService;

    #endregion

    public LoggerUnit(UnitContext unitContext, IServiceProvider serviceProvider, IConsoleService consoleService)
    {
        this.loggerResolvers = unitContext.LoggerResolvers;
        this.serviceProvider = serviceProvider;
        this.consoleService = consoleService;
    }

    public ILogger<TLogSource> GetLogger<TLogSource>()
        => this.serviceProvider.GetRequiredService<ILogger<TLogSource>>();

    public ILogger GetLogger(Type logSource)
        => (ILogger)this.serviceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(logSource));

    public ILogWriter? TryGet<TLogSource>(LogLevel logLevel = LogLevel.Information)
    {
        return this.sourceLevelToLogger.GetOrAdd(new(typeof(TLogSource), logLevel), x =>
        {
            LoggerResolverContext context = new(x);
            var resolvers = this.loggerResolvers;
            for (var i = 0; i < resolvers.Length; i++)
            {
                resolvers[i](context);
            }

            if (context.LogOutputType != null)
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

    public ILogWriter Get<TLogSource>(LogLevel logLevel = LogLevel.Information)
    {
        if (this.TryGet<TLogSource>(logLevel) is { } logger)
        {
            return logger;
        }

        throw new LoggerNotFoundException(typeof(TLogSource), logLevel);
    }

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
