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
        context.TryAddSingleton<LogUnit>();
        context.TryAddScoped<LogScope>();
        context.Services.TryAddScoped(typeof(ILogService), sp => sp.GetRequiredService<LogScope>());

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

    private ConcurrentDictionary<BufferedLogOutput, BufferedLogOutput> logOutputsToBeFlushed = new();

    #endregion

    public LogUnit()
    {
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
}
