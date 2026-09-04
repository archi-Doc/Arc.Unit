// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc;
using Arc.Threading;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;

namespace QuickStart;

public class Program
{
    private static ExecutionRoot? root;

    public static async Task Main(string[] args)
    {
        AppCloseHandler.Set(() =>
        {// Closing the console window or terminating the process.
            root?.RequestTermination(); // Send a termination signal to the root.
            root?.WaitForTermination(TimeSpan.FromSeconds(2)).Wait();
        });

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed.
            e.Cancel = true;
            root?.RequestTermination(); // Send a termination signal to the root.
        };

        var builder = new ConsoleUnit.Builder()
            .Configure(context =>
            {
                context.UnitName = "QuickUnit";

                // Add Command
                context.AddCommand<ExampleCommand, ExampleCommandOptions>();
            });

        var unit = builder.Build();
        root = unit.Context.ExecutionRoot;
        await unit.RunAsync(new(args));

        root.RequestTermination();
        if (unit.Context.ServiceProvider.GetService<LogUnit>() is { } unitLogger)
        {
            await unitLogger.FlushAndTerminate();
        }

        await root.WaitForTermination(TerminationOptions.IncludeIndependent); // Wait for the termination infinitely.
    }
}
