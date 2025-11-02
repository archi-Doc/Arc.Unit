// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Arc.Unit;

public partial class InputConsole : IConsoleService
{
    public ConsoleColor DefaultInputColor { get; set; } = (ConsoleColor)(-1);

    private readonly ObjectPool<InputBuffer> bufferPool = new(() => new InputBuffer());

    private readonly Lock lockObject = new();
    private InputBuffer current = new();

    public InputConsole()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        // Array.Fill(this.whitespace, ' ');
    }

    public string? ReadLine(string? prompt = default)
    {
        if (prompt?.Length > 0)
        {
            using (this.lockObject.EnterScope())
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
                    InputBuffer? rent = default;
                    using (this.lockObject.EnterScope())
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
                    using (this.lockObject.EnterScope())
                    {
                        this.current.Array[textPosition] = keyChar;
                        this.current.TextLength++;
                    }

                    Console.Out.Write(keyChar);
                }
            }

            string result;
            using (this.lockObject.EnterScope())
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

    private InputBuffer? StoreBuffer()
    {
        InputBuffer previous;
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
        // Console.Out.Write(this.whitespace.AsSpan(0, previous.TextLength));
        Console.CursorLeft = 0;

        return previous;
    }

    private void RestoreBuffer(InputBuffer stored)
    {
        using (this.lockObject.EnterScope())
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
