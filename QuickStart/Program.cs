// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;

namespace QuickStart;

public class Program
{
    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {// Console window closing or process terminated.
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            ThreadCore.Root.TerminationEvent.WaitOne(2000); // Wait until the termination process is complete (#1).
        };

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed
            e.Cancel = true;
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        };

        var builder = new ConsoleUnit.Builder()
            .Configure(context =>
            {
                // Add Command
                context.AddCommand(typeof(ExampleCommand));
            });

        var product = builder.Build();
        await product.RunAsync(new(args));

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        if (product.Context.ServiceProvider.GetService<UnitLogger>() is { } unitLogger)
        {
            await unitLogger.FlushAndTerminate();
        }

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
