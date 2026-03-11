// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc.Unit;

public class LogUnit
{
    internal static long OffsetTicks { get; private set; }

    public static void SetTimeOffset(TimeSpan timeSpan)
    {
        OffsetTicks = timeSpan.Ticks; // (long)(timeSpan.TotalSeconds * Stopwatch.Frequency);
    }

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

    public LogUnit(UnitContext unitContext, IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        this.loggerResolvers = unitContext.LoggerResolvers;
    }

    public bool TryRegisterFlush(BufferedLogOutput logOutput)
        => this.logOutputsToBeFlushed.TryAdd(logOutput, logOutput);

    public async Task Flush()
    {
        var logs = this.logOutputsToBeFlushed.Keys.ToArray();
        foreach (var x in logs)
        {
            await x.Flush(false).ConfigureAwait(false);
        }
    }

    public async Task FlushConsole()
    {
        var logs = this.logOutputsToBeFlushed.Keys.Where(x => x.GetType() == typeof(ConsoleLogger)).ToArray();
        foreach (var x in logs)
        {
            await x.Flush(false).ConfigureAwait(false);
        }
    }

    public async Task FlushAndTerminate()
    {
        var logs = this.logOutputsToBeFlushed.Keys.ToArray();
        foreach (var x in logs)
        {
            await x.Flush(true).ConfigureAwait(false);
        }
    }

    internal LogBroker? GetLogBroker<TLogSource>(LogLevel logLevel)
    {
        return this.brokers.GetOrAdd(new(typeof(TLogSource), logLevel), x =>
        {
            var context = new LoggerResolverContext(x);
            for (var i = 0; i < this.loggerResolvers.Length; i++)
            {
                this.loggerResolvers[i](context);
            }

            if (context.LogOutputType is not null)
            {
                if (this.serviceProvider.GetService(context.LogOutputType) is ILogOutput logOutput)
                {
                    var logFilter = context.LogFilterType == null ? null : (ILogFilter)this.serviceProvider.GetRequiredService(context.LogFilterType);
                    return new LogBroker(x.LogSourceType, x.LogLevel, logOutput, logFilter);
                }
            }

            return default;
        });
    }
}
