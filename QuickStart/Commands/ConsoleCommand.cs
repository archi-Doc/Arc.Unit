// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;

namespace QuickStart;

[SimpleCommand("console", Default = true)]
public class ConsoleCommand : ISimpleCommandAsync
{
    private readonly UnitContext unitContext;
    private readonly UnitOptions unitOptions;
    private readonly ILogger logger;
    private readonly IConsoleService consoleService;

    public ConsoleCommand(UnitContext unitContext, UnitOptions unitOptions, ILogger<ConsoleCommand> logger, IConsoleService consoleService)
    {
        this.unitContext = unitContext;
        this.unitOptions = unitOptions;
        this.logger = logger;
        this.consoleService = consoleService;
    }

    public async Task RunAsync(string[] args)
    {
        this.consoleService.WriteLine($"Name: {this.unitOptions.UnitName}", ConsoleColor.Red);
        this.consoleService.WriteLine($"Directory: {this.unitContext.Options.ProgramDirectory}");

        this.logger.GetWriter()?.Write("Console command");
        this.logger.GetWriter(LogLevel.Debug)?.Write("Start");
        this.consoleService.WriteLine("Console test");

        this.logger.GetWriter(LogLevel.Debug)?.Write("Debug");
        this.logger.GetWriter(LogLevel.Information)?.Write("Information");
        this.logger.GetWriter(LogLevel.Warning)?.Write("Warning");
        this.logger.GetWriter(LogLevel.Error)?.Write("Log filter test: Error -> Fatal");
        this.logger.GetWriter(LogLevel.Fatal)?.Write("Log filter test: Fatal -> Error");

        this.unitContext.ServiceProvider.GetRequiredService<ILogger<DefaultLog>>().GetWriter()?.Write("---");
        this.logger.GetWriter(LogLevel.Debug)?.Write("End");
    }
}
