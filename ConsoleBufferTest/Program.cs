// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace ConsoleBufferTest;

public class SimpleConsole : IConsoleService
{
    private const int BufferSize = 1_024;

    private readonly Lock lockObject = new();
    private readonly char[] buffer = new char[BufferSize];
    private int promptLength;
    private int textLength;

    public SimpleConsole()
    {
    }

    public void Flush(string? prompt = default)
    {
        string? text = default;
        using (this.lockObject.EnterScope())
        {
            if (this.textLength > 0)
            {
                text = new string(this.buffer, this.promptLength, this.textLength);
            }

            if (prompt?.Length > 0)
            {
                prompt.AsSpan(0, Math.Min(prompt.Length, BufferSize)).CopyTo(this.buffer);
                this.promptLength = prompt.Length;
                this.textLength = 0;
            }
        }

        /*if (text is not null)
        {
            Console.WriteLine(text);
        }*/

        if (prompt?.Length > 0)
        {
            Console.Write(prompt);
        }
    }

    public string? ReadLine(string? prompt = default)
    {
        this.Flush(prompt);

        try
        {
            ConsoleKeyInfo key;
            while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (this.textLength > 0)
                    {
                        this.textLength--;
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    this.buffer[this.textLength++] = key.KeyChar;
                    Console.Write(key.KeyChar);
                }
            }

            var result = new string(this.buffer, 0, this.textLength);
            this.textLength = 0;
            Console.WriteLine();
            return result;
        }
        catch
        {
            return null;
        }
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

internal class Program
{
    public static void Main(string[] args)
    {
        var consoleBuffer = new SimpleConsole();

        consoleBuffer.Write("A");
        consoleBuffer.Write("B");
        consoleBuffer.WriteLine("C");
        consoleBuffer.WriteLine("Hello, World!");

        while (true)
        {
            var input = consoleBuffer.ReadLine("> ");

            if (input == "exit")
            {// exit
                break;
            }
            else if (string.IsNullOrEmpty(input))
            {
                continue;
            }
            else
            {
                consoleBuffer.WriteLine($"Command: {input}");
            }
        }
    }
}
