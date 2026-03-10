// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

public class LogService : ILogService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IConsoleService consoleService;

    public LogUnit LogUnit { get; }

    public IConsoleService ConsoleService => this.consoleService;

    public LogService(LogUnit logUnit, IServiceProvider serviceProvider, IConsoleService consoleService)
    {
        this.LogUnit = logUnit;
        this.serviceProvider = serviceProvider;
        this.consoleService = consoleService;
    }

    public ILogger<TLogSource> GetLogger<TLogSource>()
       => this.serviceProvider.GetRequiredService<ILogger<TLogSource>>();

    public ILogWriter? GetLogWriter<TLogSource>(LogLevel logLevel = LogLevel.Information)
    {
        var broker = this.LogUnit.GetLogBroker<TLogSource>(logLevel);
        if (broker is null)
        {
            return default;
        }

        return new LogWriter(this, broker);
    }
}
