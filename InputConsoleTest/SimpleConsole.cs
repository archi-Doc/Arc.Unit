// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Arc.Unit;

public class SimpleConsole : IConsoleService
{
    private const int BufferSize = 1_024;

    internal class Buffer
    {
        public string? Prompt { get; set; }

        public char[] Array { get; } = new char[BufferSize];

        // public int PromptLength { get; set; }

        public int TextLength { get; set; }

        public int PromptLength => this.Prompt?.Length ?? 0;

        public int TotalLength => this.PromptLength + this.TextLength;

        // public int TotalLength => this.PromptLength + this.TextLength;

        // public ReadOnlySpan<char> TotalSpan => this.CharArray.AsSpan(0, this.TotalLength);

        // public ReadOnlySpan<char> PromptSpan => this.CharArray.AsSpan(0, this.PromptLength);

        // public ReadOnlySpan<char> TextSpan => this.CharArray.AsSpan(this.PromptLength, this.TextLength);

        public Span<char> TextSpan => this.Array.AsSpan(0, this.TextLength);

        public void Clear()
        {
            this.Prompt = null;
            this.TextLength = 0;
        }
    }

    private readonly char[] whitespace = new char[BufferSize];
    private readonly ObjectPool<Buffer> bufferPool = new(() => new Buffer());

    private readonly Lock currentLock = new();
    private Buffer current = new();

    public SimpleConsole()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Array.Fill(this.whitespace, ' ');
    }

    public string? ReadLine(string? prompt = default)
    {
        if (prompt?.Length > 0)
        {
            using (this.currentLock.EnterScope())
            {
                this.current.Prompt = prompt;
            }

            Console.Out.Write(prompt);
        }

        try
        {
            Span<char> surrogatePair = stackalloc char[2];
            while (true)
            {
                var keyInfo = Console.ReadKey(intercept: true);
                var key = keyInfo.Key;
                var keyChar = keyInfo.KeyChar;
                var cursorLeft = Console.CursorLeft;
                var textPosition = cursorLeft - this.current.PromptLength;

                if (key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (key == ConsoleKey.Backspace)
                {
                    Buffer? rent = default;
                    using (this.currentLock.EnterScope())
                    {
                        if (textPosition > 0)
                        {
                            var sourceSpan = this.current.TextSpan.Slice(textPosition);

                            rent = this.bufferPool.Rent();
                            rent.Clear();
                            sourceSpan.CopyTo(rent.Array.AsSpan());
                            rent.Array[sourceSpan.Length] = ' ';
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
                    // if (Console.CursorLeft < this.current.TotalLength)
                    {
                        Console.CursorLeft++;
                    }
                }
                else
                {
                    using (this.currentLock.EnterScope())
                    {
                        this.current.Array[textPosition] = keyChar;
                        this.current.TextLength++;
                    }

                    Console.Out.Write(keyChar);
                }
            }

            string result;
            using (this.currentLock.EnterScope())
            {
                result = this.current.TextSpan.ToString();
                this.current.Clear();
            }

            Console.Out.WriteLine();
            return result;
        }
        catch
        {
            return null;
        }
    }

    public void Write(string? message = null)
    {
        if (Environment.NewLine == "\r\n" && message is not null)
        {
            message = Arc.BaseHelper.ConvertLfToCrLf(message);
        }

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
        if (Environment.NewLine == "\r\n" && message is not null)
        {
            message = Arc.BaseHelper.ConvertLfToCrLf(message);
        }

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
        using (this.currentLock.EnterScope())
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
        using (this.currentLock.EnterScope())
        {
            this.current.Prompt = stored.Prompt;
            this.current.TextLength = stored.TextLength;
            stored.TextSpan.CopyTo(this.current.TextSpan);
        }

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
