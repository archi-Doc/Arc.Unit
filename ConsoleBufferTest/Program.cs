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

        public int PromptLength => this.Prompt?.Length ?? 0;

        public int TotalLength => this.PromptLength + this.TextLength;

        // public int TotalLength => this.PromptLength + this.TextLength;

        // public ReadOnlySpan<char> TotalSpan => this.CharArray.AsSpan(0, this.TotalLength);

        // public ReadOnlySpan<char> PromptSpan => this.CharArray.AsSpan(0, this.PromptLength);

        // public ReadOnlySpan<char> TextSpan => this.CharArray.AsSpan(this.PromptLength, this.TextLength);

        public Span<char> TextSpan => this.CharArray.AsSpan(0, this.TextLength);

        public void Clear()
        {
            this.Prompt = null;
            this.TextLength = 0;
        }
    }

    private readonly char[] whitespace = new char[BufferSize];
    private readonly Lock lockObject = new();
    private Buffer current = new();

    private ObjectPool<Buffer> bufferPool = new(() => new Buffer());

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
            using (this.lockObject.EnterScope())
            {
                this.current.Prompt = prompt;
            }

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
                        var textPosition = cursorLeft - this.current.PromptLength;
                        if (textPosition > 0)
                        {
                            var sourceSpan = this.current.TextSpan.Slice(textPosition);

                            rent = this.bufferPool.Rent();
                            rent.Clear();
                            sourceSpan.CopyTo(rent.CharArray.AsSpan());
                            rent.CharArray[sourceSpan.Length] = ' ';
                            rent.TextLength = sourceSpan.Length + 1;

                            sourceSpan.CopyTo(this.current.TextSpan.Slice(textPosition - 1));
                            this.current.TextLength--;

                            cursorLeft = this.current.PromptLength + textPosition - 1;
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
                    if (Console.CursorLeft > this.current.PromptLength)
                    {
                        Console.CursorLeft--;
                    }
                }
                else if (key == ConsoleKey.RightArrow)
                {
                    if (Console.CursorLeft < this.current.PromptLength)
                    {
                        Console.CursorLeft++;
                    }
                }
                else
                {
                    var cursorLeft = Console.CursorLeft;
                    this.current.TextSpan[cursorLeft] = keyChar;
                    this.current.TextLength++;
                    Console.Write(keyChar);
                }
            }

            string result;
            using (this.lockObject.EnterScope())
            {
                result = this.current.TextSpan.ToString();
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

    private Buffer? StoreBuffer()
    {
        Buffer previous;
        using (this.lockObject.EnterScope())
        {
            if (this.current.TextLength > 0 || this.current.Prompt is not null)
            {
                previous = this.current;
                this.current = this.bufferPool.Rent();
                this.current.Clear();
            }
            else
            {
                return null;
            }
        }

        Console.CursorLeft = 0;
        Console.Out.Write(this.whitespace.AsSpan(0, previous.TextLength));
        Console.CursorLeft = 0;

        return previous;
    }

    private void RestoreBuffer(Buffer stored)
    {
        using (this.lockObject.EnterScope())
        {
            if (stored.Prompt is not null)
            {
                Console.Out.Write(stored.Prompt);
            }

            if (stored.TextLength > 0)
            {
                Console.Out.Write(stored.TextSpan);
            }

            this.bufferPool.Return(stored);
        }
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
