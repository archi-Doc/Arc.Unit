// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;

namespace Sandbox;

public interface ITestInterface
{
}

public interface ITestInterface<T> : ITestInterface
{
}

public class TestClass : ITestInterface
{
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
            ThreadCore.Root.TerminationEvent.WaitOne(2000); // Wait until the termination process is complete (#1).
        };

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed
            e.Cancel = true;
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        };

        var builder = new UnitBuilder()
            .Configure(context =>
            {
                context.AddSingleton<ITestInterface, TestClass>();
                context.Services.Add(ServiceDescriptor.Singleton(typeof(ITestInterface<>), typeof(TestClassFactory<>)));
                // context.Services.Add(ServiceDescriptor.Singleton(typeof(ITestInterface<>).MakeGenericType(typeof(int)), new TestClass()));
            });

        var unit = builder.Build();

        var obj = unit.Context.ServiceProvider.GetRequiredService<ITestInterface>();
        var obj2 = unit.Context.ServiceProvider.GetRequiredService<ITestInterface<int>>();

        ThreadCore.Root.Terminate();
        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
        unit.Context.ServiceProvider.GetService<UnitLogger>()?.FlushAndTerminate();
        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
