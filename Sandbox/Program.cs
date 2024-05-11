// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Threading;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;

namespace Sandbox;

public record TestOptions
{
    public string Name { get; init; } = string.Empty;
}

public interface ITestInterface
{
}

public interface ITestInterface<T> : ITestInterface
{
}

public class CustomContext : IUnitCustomContext
{
    void IUnitCustomContext.Configure(IUnitConfigurationContext context)
    {
    }
}

public class TestClass : ITestInterface
{
    public TestClass(TestOptions options)
    {
        this.options = options;
    }

    private TestOptions options;
}

public class TestClassFactory<T> : ITestInterface<T>
{
}

public class Program
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
                var custom = context.GetCustomContext<CustomContext>();
                context.AddSingleton<TestOptions>();
                context.AddSingleton<ITestInterface, TestClass>();
                context.Services.Add(ServiceDescriptor.Singleton(typeof(ITestInterface<>), typeof(TestClassFactory<>)));
                // context.Services.Add(ServiceDescriptor.Singleton(typeof(ITestInterface<>).MakeGenericType(typeof(int)), new TestClass()));

                // Logger
                context.ClearLoggerResolver();
                context.AddLoggerResolver(x =>
                {// Log source/level -> Resolver() -> Output/filter
                    if (x.LogLevel <= LogLevel.Debug)
                    {
                        // x.SetOutput<ConsoleLogger>();
                        return;
                    }

                    // x.SetOutput<MemoryLogger>();
                    x.SetOutput<ConsoleAndFileLogger>();
                });
            })
            .SetupOptions<TestOptions>((context, options) =>
            {
                // options.Name = "test";
                // context.SetOptionsForUnitContext(new TestOptions() with { Name = "test", });
            })
            .SetupOptions<FileLoggerOptions>((context, options) =>
            {// FileLoggerOptions
                var logfile = "Logs/TestLog.txt";
                options.Path = Path.Combine(context.RootDirectory, logfile);
                options.MaxLogCapacity = 1;
            })
            .SetupOptions<ConsoleLoggerOptions>((context, options) =>
            {
                options.EnableBuffering = true;
            });

        var builder2 = new UnitBuilder()
           .Configure(context =>
           {
           });
        builder.AddBuilder(builder2);

        var unit = builder.Build();

        var obj = unit.Context.ServiceProvider.GetRequiredService<ITestInterface>();
        var obj2 = unit.Context.ServiceProvider.GetRequiredService<ITestInterface<int>>();

        var unitLogger = unit.Context.ServiceProvider.GetRequiredService<UnitLogger>();
        var logger = unitLogger.GetLogger<TestClass>();

        var fileLogger = unit.Context.ServiceProvider.GetRequiredService<FileLogger<FileLoggerOptions>>();
        var path = fileLogger.GetCurrentPath();

        Parallel.For(0, 5, x =>
        {
            for (var i = 0; i < 10; i++)
            {
                logger.TryGet()?.Log($"{x} - {i}");
            }
        });

        var ff = PathHelper.RunningInContainer;
        ff = PathHelper.RunningInContainer;

        var memoryLogger = unit.Context.ServiceProvider.GetRequiredService<MemoryLogger>();
        var array = memoryLogger.ToArray();
        var st = Encoding.UTF8.GetString(array);

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        unit.Context.ServiceProvider.GetService<UnitLogger>()?.FlushAndTerminate();
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
