// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace ConsoleBufferTest;

internal class Program
{
    public static void Main(string[] args)
    {
        var inputConsole = new InputConsole();
        // var simpleConsole = new SimpleConsole();
        // Console.In = simpleConsole;

        inputConsole.Write("A");
        inputConsole.Write("B");
        inputConsole.WriteLine("C");
        inputConsole.WriteLine("Hello, World!");

        while (true)
        {
            var input = inputConsole.ReadLine($"{Console.CursorTop}> ");

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
                    inputConsole.WriteLine("AAAAA");
                });
            }
            else
            {
                inputConsole.WriteLine($"Command: {input}");
            }
        }
    }
}
