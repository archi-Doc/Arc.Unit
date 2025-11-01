// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Arc.Collections;
using Arc.Unit;

namespace ConsoleBufferTest;

public class SimpleConsole : IConsoleService
{
    private const int BufferSize = 1_024;

    internal class Buffer
    {
        public string? Prompt { get; set; }

        public char[] CharArray { get; } = new char[BufferSize];

        // public int PromptLength { get; set; }

        public int TextLength { get; set; }

        // public int TotalLength => this.PromptLength + this.TextLength;

        // public ReadOnlySpan<char> TotalSpan => this.CharArray.AsSpan(0, this.TotalLength);

        // public ReadOnlySpan<char> PromptSpan => this.CharArray.AsSpan(0, this.PromptLength);

        // public ReadOnlySpan<char> TextSpan => this.CharArray.AsSpan(this.PromptLength, this.TextLength);

        public ReadOnlySpan<char> TextSpan => this.CharArray.AsSpan(0, this.TextLength);
    }

    private readonly Lock lockObject = new();
    private readonly char[] whitespace = new char[BufferSize];
    private readonly char[] charArray = new char[BufferSize];
    private int textLength;

    private ObjectPool<Buffer> bufferPool = new(() => new Buffer());

    private int BufferLength => this.promptLength + this.textLength;

    public SimpleConsole()
    {
        Array.Fill(this.whitespace, ' ');
    }

    /*public void Flush(string? prompt = default)
    {
        string? text = default;
        using (this.lockObject.EnterScope())
        {
            if (this.textLength > 0)
            {
                text = new string(this.charArray, this.promptLength, this.textLength);
            }

            if (prompt?.Length > 0)
            {
                prompt.AsSpan(0, Math.Min(prompt.Length, BufferSize)).CopyTo(this.charArray);
                this.promptLength = prompt.Length;
                this.textLength = 0;
            }
        }

        if (prompt?.Length > 0)
        {
            Console.Write(prompt);
        }
    }*/

    public string? ReadLine(string? prompt = default)
    {
        if (prompt?.Length > 0)
        {
            Console.Write(prompt);
        }

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
                    Buffer? rent = default;
                    var cursorLeft = Console.CursorLeft;
                    using (this.lockObject.EnterScope())
                    {
                        var textPosition = cursorLeft - this.promptLength;
                        if (textPosition > 0)
                        {
                            var sourceSpan = this.charArray.AsSpan(this.promptLength + textPosition, this.textLength - textPosition);

                            rent = this.bufferPool.Rent();
                            sourceSpan.CopyTo(rent.CharArray.AsSpan());
                            rent.CharArray[sourceSpan.Length] = ' ';
                            rent.TextLength = sourceSpan.Length + 1;

                            sourceSpan.CopyTo(this.charArray.AsSpan(this.promptLength + textPosition - 1));
                            this.textLength--;

                            cursorLeft = this.promptLength + textPosition - 1;
                        }
                    }

                    if (rent is not null)
                    {
                        Console.CursorLeft = cursorLeft;
                        Console.Out.Write(rent.TextSpan);
                        Console.CursorLeft = cursorLeft;

                        this.bufferPool.Return(rent);
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
                    this.charArray[cursorLeft] = keyChar;
                    this.textLength++;
                    Console.Write(keyChar);
                }
            }

            string result;
            using (this.lockObject.EnterScope())
            {
                result = new string(this.charArray, this.promptLength, this.textLength);
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
        var stored = this.StoreBuffer();

        try
        {
            if (stored is not null)
            {
                Console.CursorLeft = 0;
                Console.Out.Write(this.whitespace.AsSpan(0, stored.Length));
                Console.CursorLeft = 0;
            }

            Console.WriteLine(message);

            if (stored is not null)
            {
                this.RestoreBuffer(stored);
            }
        }
        catch
        {
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

    private string? StoreBuffer()
    {
        using (this.lockObject.EnterScope())
        {
            if (this.BufferLength > 0)
            {
                var escaped = new string(this.charArray, 0, this.BufferLength);
                this.ClearBufferInternal();
                return escaped;
            }
            else
            {
                return null;
            }
        }
    }

    private void RestoreBuffer(string stored)
    {
        using (this.lockObject.EnterScope())
        {
            var length = Math.Min(stored.Length, BufferSize);
            stored.AsSpan(0, length).CopyTo(this.charArray);
            this.promptLength = length;
            this.textLength = 0;
            Console.Write(stored);
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
