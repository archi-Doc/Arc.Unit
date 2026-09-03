// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Unit;
using SimpleCommandLine;

namespace QuickStart;

[SimpleCommand("example")]
public class ExampleCommand : ISimpleCommand<ExampleCommandOptions>
{
    // SimpleCommandLine binds the command-line arguments to the members of the option class by reflection,
    // so the members must be preserved when the application is trimmed or compiled with Native AOT.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ExampleCommandOptions))]
    public ExampleCommand(ILogger<ExampleCommand> logger)
    {
        this.logger = logger;
    }

    public async Task Execute(ExampleCommandOptions option, string[] args, CancellationToken cancellationToken)
    {
        this.logger.GetWriter(LogLevel.Debug)?.Write($"Example command: {option.String}");
    }

    private readonly ILogger logger;
}

public record ExampleCommandOptions
{
    [SimpleOption("string", Description = "String", Required = true)]
    public string String { get; init; } = string.Empty;
}
