// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

public class UnitLogger
{
    internal static long OffsetTicks { get; private set; }

    public static void SetTimeOffset(TimeSpan timeSpan)
    {
        OffsetTicks = timeSpan.Ticks; // (long)(timeSpan.TotalSeconds * Stopwatch.Frequency);
    }

    public static void Configure(IUnitConfigurationContext context)
    {
        // Main
        context.TryAddSingleton<UnitLogger>();
        context.Services.Add(ServiceDescriptor.Singleton<ILogWriter>(x => x.GetService<UnitLogger>()?.Get<DefaultLog>() ?? throw new LoggerNotFoundException(typeof(DefaultLog), LogLevel.Information)));
        context.Services.Add(ServiceDescriptor.Singleton(typeof(ILogger), typeof(LoggerFactory<DefaultLog>)));
        context.Services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(LoggerFactory<>)));

        // Empty logger
        context.TryAddSingleton<EmptyLogger>();

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

    private class LogContext : ILogContext
    {
        public LogContext(UnitLogger unitLogger)
        {
            this.unitLogger = unitLogger;
        }

        public ILogWriter? TryGet<TLogOutput>(LogLevel logLevel)
        {
            return this.unitLogger.sourceLevelToLogger.GetOrAdd(new(typeof(TLogOutput), logLevel), x =>
            {
                if (this.unitLogger.serviceProvider.GetService(x.LogSourceType) is ILogOutput logOutput)
                {
                    return new LogInstance(this, null!, x.LogLevel, logOutput, null);
                }

                return null;
            });
        }

        private UnitLogger unitLogger;
    }

    public UnitLogger(UnitContext context)
    {
        this.UnitContext = context;
        this.logContext = new(this);
        this.serviceProvider = context.ServiceProvider;
        this.loggerResolvers = (LoggerResolverDelegate[])context.LoggerResolvers.Clone();
    }

    public ILogger<TLogSource> GetLogger<TLogSource>()
        => this.serviceProvider.GetRequiredService<ILogger<TLogSource>>();

    public ILogger GetLogger(Type logSource)
        => (ILogger)this.serviceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(logSource));

    public ILogWriter? TryGet<TLogSource>(LogLevel logLevel = LogLevel.Information)
    {
        return this.sourceLevelToLogger.GetOrAdd(new(typeof(TLogSource), logLevel), x =>
        {
            LoggerResolverContext context = new(this, x);
            for (var i = 0; i < this.loggerResolvers.Length; i++)
            {
                this.loggerResolvers[i](context);
            }

            if (context.LogOutputType != null)
            {
                if (this.serviceProvider.GetService(context.LogOutputType) is ILogOutput logOutput)
                {
                    var logFilter = context.LogFilterType == null ? null : (ILogFilter)this.serviceProvider.GetRequiredService(context.LogFilterType);
                    return new LogInstance(this.logContext, x.LogSourceType, x.LogLevel, logOutput, logFilter);
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

    internal UnitContext UnitContext { get; }

    private LogContext logContext;
    private IServiceProvider serviceProvider;
    private LoggerResolverDelegate[] loggerResolvers;
    private ConcurrentDictionary<LogSourceLevelPair, ILogWriter?> sourceLevelToLogger = new();
    private ConcurrentDictionary<BufferedLogOutput, BufferedLogOutput> logsToFlush = new();
}
