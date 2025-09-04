// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using SimpleCommandLine;

namespace QuickStart;

[SimpleCommand("console", Default = true)]
public class ConsoleCommand : ISimpleCommandAsync
{
    public ConsoleCommand(ILogger<ConsoleCommand> logger, IConsoleService consoleService)
    {
        this.logger = logger;
        this.consoleService = consoleService;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet()?.Log("Console command");
        this.logger.TryGet(LogLevel.Debug)?.Log("Start");
        this.consoleService.WriteLine("Console test");

        this.logger.TryGet(LogLevel.Error)?.Log("Log filter test: Error -> Fatal");
        this.logger.TryGet(LogLevel.Fatal)?.Log("Log filter test: Fatal -> Error");

        this.logger.TryGet(LogLevel.Debug)?.Log("End");
    }

    private readonly ILogger logger;
    private readonly IConsoleService consoleService;
}
