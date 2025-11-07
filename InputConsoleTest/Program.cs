// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleBufferTest;

internal class Program
{
    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {// Console window closing or process terminated.
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            ThreadCore.Root.TerminationEvent.WaitOne(2_000); // Wait until the termination process is complete (#1).
        };

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed
            e.Cancel = true;
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        };

        var builder = new UnitBuilder()
            .Configure(context =>
            {
                context.AddLoggerResolver(x =>
                {
                    x.SetOutput<FileLogger<FileLoggerOptions>>();
                    return;
                });
            })
            .PostConfigure(context =>
            {
                var logfile = "Logs/Log.txt";
                context.SetOptions(context.GetOptions<FileLoggerOptions>() with
                {
                    Path = Path.Combine(context.ProgramDirectory, logfile),
                    MaxLogCapacity = 1,
                });
            });

        var product = builder.Build();
        var logger = product.Context.ServiceProvider.GetRequiredService<ILogger<DefaultLog>>();
        logger.TryGet()?.Log("Start");

        var inputConsole = new InputConsole();
        inputConsole.Logger = product.Context.ServiceProvider.GetRequiredService<ILogger<InputConsole>>();
        // var simpleConsole = new SimpleConsole();
        // Console.In = simpleConsole;

        inputConsole.Write("A");
        inputConsole.Write("B");
        inputConsole.WriteLine("C");
        inputConsole.WriteLine("Hello, World!");

        while (!ThreadCore.Root.IsTerminated)
        {
            /*if (!Console.KeyAvailable)
            {
                await Task.Delay(100);
                continue;
            }*/

            var input = inputConsole.ReadLine($"{Console.CursorTop}> ");

            if (string.Equals(input, "exit", StringComparison.InvariantCultureIgnoreCase))
            {// exit
                ThreadCore.Root.Terminate(); // Send a termination signal to the root.
                break;
            }
            else if (string.IsNullOrEmpty(input))
            {// continue
                continue;
            }
            else if (string.Equals(input, "a", StringComparison.InvariantCultureIgnoreCase))
            {
                _ = Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    inputConsole.WriteLine("AAAAA");
                });
            }
            else
            {
                inputConsole.WriteLine($"Command: {input}");
            }
        }

        inputConsole.Abort();

        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        if (product.Context.ServiceProvider.GetService<UnitLogger>() is { } unitLogger)
        {
            logger.TryGet()?.Log("End");
            await unitLogger.FlushAndTerminate();
        }

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
