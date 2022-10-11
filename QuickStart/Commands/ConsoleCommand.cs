// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using SimpleCommandLine;

namespace QuickStart;

[SimpleCommand("console", Default = true)]
public class ConsoleCommand : ISimpleCommandAsync
{
    public ConsoleCommand(ILogger<ConsoleCommand> logger)
    {
        this.logger = logger;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.TryGet()?.Log("Console command");
        this.logger.TryGet(LogLevel.Debug)?.Log("Start");

        this.logger.TryGet(LogLevel.Error)?.Log("Log filter test: Error -> Fatal");

        this.logger.TryGet(LogLevel.Debug)?.Log("End");
    }

    private ILogger<ConsoleCommand> logger;
}
