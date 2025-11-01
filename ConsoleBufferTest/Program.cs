// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace ConsoleBufferTest;

internal class Program
{
    public static void Main(string[] args)
    {
        var simpleConsole = new SimpleConsole();
        // Console.In = simpleConsole;

        simpleConsole.Write("A");
        simpleConsole.Write("B");
        simpleConsole.WriteLine("C");
        simpleConsole.WriteLine("Hello, World!");

        while (true)
        {
            var input = simpleConsole.ReadLine($"{Console.CursorTop}> ");

            if (string.Equals(input, "exit", StringComparison.InvariantCultureIgnoreCase))
            {// exit
                break;
            }
            else if (string.IsNullOrEmpty(input))
            {// continue
                continue;
            }
            else if (string.Equals(input, "a", StringComparison.InvariantCultureIgnoreCase))
            {
                Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    simpleConsole.WriteLine("AAAAA");
                });
            }
            else
            {
                simpleConsole.WriteLine($"Command: {input}");
            }
        }
    }
}
