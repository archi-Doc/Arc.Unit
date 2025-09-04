// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using SimpleCommandLine;

namespace QuickStart;

[SimpleCommand("example")]
public class ExampleCommand : ISimpleCommandAsync<ExampleCommandOptions>
{
    public ExampleCommand(ILogger<ExampleCommand> logger)
    {
        this.logger = logger;
    }

    public async Task RunAsync(ExampleCommandOptions option, string[] args)
    {
        this.logger.TryGet(LogLevel.Debug)?.Log($"Example command: {option.String}");
    }

    private readonly ILogger logger;
}

public record ExampleCommandOptions
{
    [SimpleOption("string", Description = "String", Required = true)]
    public string String { get; init; } = string.Empty;
}
