// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc;
using Arc.Threading;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;

namespace Sandbox;

public record TestOptions
{
    public string Name { get; set; } = string.Empty;
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
    private static ExecutionRoot? root;

    public static async Task Main(string[] args)
    {
        AppCloseHandler.Register(() =>
        {// Closing the console window or terminating the process.
            root?.RequestTermination(); // Send a termination signal to the root.
            root?.WaitForTerminationAsync(TimeSpan.FromSeconds(2)).Wait();
        });

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed.
            e.Cancel = true;
            root?.RequestTermination(); // Send a termination signal to the root.
        };

        var builder = new UnitBuilder()
            .PreConfigure(context =>
            {
                // context.SetOptions(context.GetOrCreateOptions<TestOptions>());
            })
            .Configure(context =>
            {
                var custom = context.GetCustomContext<CustomContext>();
                context.AddSingleton<TestOptions>();
                context.AddSingleton<ITestInterface, TestClass>();
                context.Services.Add(ServiceDescriptor.Singleton(typeof(ITestInterface<>), typeof(TestClassFactory<>)));
                // context.Services.Add(ServiceDescriptor.Singleton(typeof(ITestInterface<>).MakeGenericType(typeof(int)), new TestClass()));

                // Log output
                context.ClearLogOutputResolvers();
                context.AddLogOutputResolver(x =>
                {// Log source/level -> Resolver() -> Output/filter
                    if (x.LogLevel <= LogLevel.Debug)
                    {
                        // x.SetOutput<ConsoleLogOutput>();
                        return;
                    }

                    // x.SetOutput<MemoryLogOutput>();
                    x.SetOutput<ConsoleAndFileLogOutput>();
                });
            })
            .PostConfigure(context =>
            {
                context.SetOptions(context.GetOrCreateOptions<TestOptions>() with
                {
                    Name = "test",
                });

                var logfile = "Logs/TestLog.txt";
                context.SetOptions(context.GetOrCreateOptions<FileLogOutputOptions>() with
                {
                    FilePath = Path.Combine(context.ProgramDirectory, logfile),
                    MaxLogCapacityInMegabytes = 1,
                });

                context.SetOptions(context.GetOrCreateOptions<ConsoleLogOutputOptions>() with
                {
                    EnableBuffering = true,
                });
            });

        var builder2 = new UnitBuilder()
            .PostConfigure(context =>
            {
                context.UnitName = "mod";
            });
        builder.AddBuilder(builder2);
        builder.AddBuilder(builder2);

        var unit = builder.Build("-datadirectory 'a'");
        root = unit.Context.ExecutionRoot;

        var logger2 = unit.Context.ServiceProvider.GetRequiredService<ILogger<ITestInterface>>();

        var obj = unit.Context.ServiceProvider.GetRequiredService<ITestInterface>();
        // A reference type argument is used, since Microsoft.Extensions.DependencyInjection cannot create
        // an open generic service with a value type argument (ITestInterface<int>) on Native AOT.
        var obj2 = unit.Context.ServiceProvider.GetRequiredService<ITestInterface<string>>();

        var logUnit = unit.Context.ServiceProvider.GetRequiredService<LogUnit>();
        var logService = unit.Context.ServiceProvider.GetRequiredService<ILogService>();
        var logger = logService.GetLogger<TestClass>();
        logger.GetWriter(LogLevel.Debug)?.Write($"Debug{ThrowException()}");
        logger.GetWriter(LogLevel.Information)?.Write($"Info");
        var logger3 = logService.GetLogger(typeof(TestClass));
        var b = logger3.Equals(logger);
        logger.GetWriter(LogLevel.Information)?.Write(b.ToString());
        var d = logUnit.RootLogService;
        logUnit.RootLogService.GetLogger<TestClass>().GetWriter()?.Write("A");

        var fileLogOutput = unit.Context.ServiceProvider.GetRequiredService<FileLogOutput<FileLogOutputOptions>>();
        var path = fileLogOutput.GetCurrentPath();

        Parallel.For(0, 5, x =>
        {
            for (var i = 0; i < 3; i++)
            {
                logger.GetWriter()?.Write($"{x} - {i}");
            }
        });

        var ff = PathHelper.IsRunningInContainer;
        ff = PathHelper.IsRunningInContainer;

        var memoryLogOutput = unit.Context.ServiceProvider.GetRequiredService<MemoryLogOutput>();
        var array = memoryLogOutput.ToUtf8Array();
        var st = Encoding.UTF8.GetString(array);

        try
        {
            await Task.Delay(600, root.CancellationToken);
            Console.WriteLine("...");
            await Task.Delay(600, root.CancellationToken);
            Console.WriteLine("...");
            await Task.Delay(600, root.CancellationToken);
            Console.WriteLine("...");
        }
        catch (OperationCanceledException)
        {
        }

        var consoleService = unit.Context.ServiceProvider.GetRequiredService<IConsoleService>();

        root.RequestTermination();
        await logUnit.FlushAndTerminateAsync();
        await root.WaitForTerminationAsync(TerminationOptions.IncludeIndependent); // Wait for the termination infinitely.
        if (unit.Context.ServiceProvider is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }

        string ThrowException()
        {
            throw new Exception();
        }
    }
}
