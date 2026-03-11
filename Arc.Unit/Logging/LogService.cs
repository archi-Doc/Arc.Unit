// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

internal class LogService : ILogService
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

    public ILogger GetLogger(Type logSource)
       => (ILogger)this.serviceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(logSource));

    public LogWriter? GetWriter<TLogSource>(LogLevel logLevel = LogLevel.Information)
    {
        var broker = this.LogUnit.GetLogBroker<TLogSource>(logLevel);
        if (broker is null)
        {
            return default;
        }

        return new(this, broker);
    }
}
