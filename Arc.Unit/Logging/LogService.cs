// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

public class LogService : ILogService
{
    private readonly LoggerResolverDelegate[] loggerResolvers;
    private readonly IServiceProvider serviceProvider;
    private readonly IConsoleService consoleService;

    private ConcurrentDictionary<LogSourceLevelPair, ILogWriter?> sourceLevelToLogger = new();

    public LogUnit LogUnit { get; }

    public LogService(UnitContext unitContext, LogUnit logUnit, IServiceProvider serviceProvider, IConsoleService consoleService)
    {
        this.loggerResolvers = unitContext.LoggerResolvers;
        this.LogUnit = logUnit;
        this.serviceProvider = serviceProvider;
        this.consoleService = consoleService;
    }

    public IConsoleService ConsoleService => this.consoleService;

    public ILogger<TLogSource> GetLogger<TLogSource>()
       => this.serviceProvider.GetRequiredService<ILogger<TLogSource>>();

    public ILogWriter? GetLogWriter<TLogSource>(LogLevel logLevel = LogLevel.Information)
    {
        return this.sourceLevelToLogger.GetOrAdd(new(typeof(TLogSource), logLevel), x =>
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
                    return new LogWriter(this, x.LogSourceType, x.LogLevel, logOutput, logFilter);
                }
            }

            return null;
        });
    }
}
