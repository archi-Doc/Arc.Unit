// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class ConsoleService : IConsoleService
{
    public ConsoleService()
    {
    }

    public void Write(string? message = null)
    {
        try
        {
            Console.Out.Write(message);
        }
        catch
        {
        }
    }

    public void WriteLine(string? message = null)
    {
        try
        {
            Console.Out.WriteLine(message);
        }
        catch
        {
        }
    }

    public InputResult ReadLine(string? prompt)
    {
        try
        {
            if (prompt is not null)
            {
                Console.Write(prompt);
            }

            var text = Console.ReadLine();
            return new(text ?? string.Empty);
        }
        catch
        {
            return new(InputResultKind.Terminated);
        }
    }

    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        try
        {
            return Console.ReadKey();
        }
        catch
        {
            return default;
        }
    }

    public bool KeyAvailable
    {
        get
        {
            try
            {
                return Console.KeyAvailable;
            }
            catch
            {
                return false;
            }
        }
    }
}
