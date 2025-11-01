// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Collections;
using Arc.Unit;

namespace ConsoleBufferTest;

public class SimpleConsole : IConsoleService
{
    private const int BufferSize = 1_024;

    private readonly Lock lockObject = new();
    private readonly char[] whitespaceBuffer = new char[BufferSize];
    private readonly char[] buffer = new char[BufferSize];
    private int promptLength;
    private int textLength;

    private ObjectPool<char[]> stringPool = new(() => new char[BufferSize]);

    private int BufferLength => this.promptLength + this.textLength;

    public SimpleConsole()
    {
        Array.Fill(this.whitespaceBuffer, ' ');
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
            while (true)
            {
                var keyInfo = Console.ReadKey(intercept: true);
                var key = keyInfo.Key;
                var keyChar = keyInfo.KeyChar;

                if (key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (key == ConsoleKey.Backspace)
                {
                    char[]? rentString = default;
                    int rentLength = 0;
                    var cursorTop = Console.CursorTop;
                    var cursorLeft = Console.CursorLeft;
                    using (this.lockObject.EnterScope())
                    {
                        var textPosition = cursorLeft - this.promptLength;
                        if (textPosition > 0)
                        {
                            var sourceSpan = this.buffer.AsSpan(this.promptLength + textPosition, this.textLength - textPosition);

                            rentString = this.stringPool.Rent();
                            sourceSpan.CopyTo(rentString.AsSpan());
                            rentString[sourceSpan.Length] = ' ';
                            rentLength = sourceSpan.Length + 1;

                            sourceSpan.CopyTo(this.buffer.AsSpan(this.promptLength + textPosition - 1));
                            this.textLength--;

                            cursorLeft = this.promptLength + textPosition - 1;
                        }
                    }

                    if (rentString is not null)
                    {
                        Console.CursorLeft = cursorLeft;
                        Console.Out.Write(rentString.AsSpan(0, rentLength));
                        Console.CursorLeft = cursorLeft;

                        this.stringPool.Return(rentString);
                    }

                    continue;
                }
                else if (key == ConsoleKey.LeftArrow)
                {
                    if (Console.CursorLeft > this.promptLength)
                    {
                        Console.CursorLeft--;
                    }
                }
                else if (key == ConsoleKey.RightArrow)
                {
                    if (Console.CursorLeft < this.BufferLength)
                    {
                        Console.CursorLeft++;
                    }
                }
                else
                {
                    var cursorLeft = Console.CursorLeft;
                    this.buffer[cursorLeft] = keyChar;
                    this.textLength++;
                    Console.Write(keyChar);
                }
            }

            string result;
            using (this.lockObject.EnterScope())
            {
                result = new string(this.buffer, this.promptLength, this.textLength);
                this.ClearBufferInternal();
            }

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
            Console.Out.Write(message);
        }
        catch
        {
        }
    }

    public void WriteLine(string? message = null)
    {
        var escaped = this.EscapeBuffer();

        try
        {
            if (escaped is not null)
            {
                Console.CursorLeft = 0;
                Console.Out.Write(this.whitespaceBuffer.AsSpan(0, escaped.Length));
                Console.CursorLeft = 0;
            }

            Console.WriteLine(message);

            if (escaped is not null)
            {
                Console.Out.Write(escaped);
            }
        }
        catch
        {
        }
    }

    public string? ReadLine()
    {
        return this.ReadLine(default);
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

    public string? EscapeBuffer()
    {
        using (this.lockObject.EnterScope())
        {
            if (this.BufferLength > 0)
            {
                var escaped = new string(this.buffer, 0, this.BufferLength);
                this.ClearBufferInternal();
                return escaped;
            }
            else
            {
                return null;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearBufferInternal()
    {
        this.promptLength = 0;
        this.textLength = 0;
    }
}

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

            if (input == "exit")
            {// exit
                break;
            }
            else if (string.IsNullOrEmpty(input))
            {
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
