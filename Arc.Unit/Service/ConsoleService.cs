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
            Console.Write(message);
        }
        catch
        {
        }
    }

    public void WriteLine(string? message = null)
    {
        try
        {
            Console.WriteLine(message);
        }
        catch
        {
        }
    }

    public string? ReadLine()
    {
        try
        {
            return Console.ReadLine();
        }
        catch
        {
            return null;
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
