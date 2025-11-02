// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Arc.Unit;

public partial class InputConsole : IConsoleService
{
    private const int KeyBufferSize = 16;

    public ConsoleColor DefaultInputColor { get; set; }

    private readonly ObjectPool<InputBuffer> bufferPool = new(() => new InputBuffer(), 32);

    private readonly Lock lockObject = new();
    private int startingCursorTop;
    private List<InputBuffer> buffers = new();

    public InputConsole(ConsoleColor inputColor = (ConsoleColor)(-1))
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        this.DefaultInputColor = inputColor;
    }

    public string? ReadLine(string? prompt = default)
    {
        InputBuffer? buffer;
        Span<char> keyBuffer = stackalloc char[KeyBufferSize];
        var position = 0;

        using (this.lockObject.EnterScope())
        {
            this.ReturnAllBuffersInternal();
            buffer = this.RentBuffer();
            buffer.SetPrompt(prompt);
            this.buffers.Add(buffer);
            this.startingCursorTop = Console.CursorTop;
        }

        if (!string.IsNullOrEmpty(prompt))
        {
            Console.Out.Write(prompt);
        }

        buffer = default;
        (int Left, int Top) cursorPos = default;
        while (true)
        {
            ConsoleKey key;
            char keyChar;
            try
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;
                keyChar = keyInfo.KeyChar;
            }
            catch
            {
                key = ConsoleKey.Enter;
                keyChar = '\0';
            }

            if (buffer is null)
            {
                cursorPos = Console.GetCursorPosition();
                this.FindBuffer(cursorPos.Top);

                if (buffer is null)
                {
                    return null;
                }
            }

            var flush = false;
            if (Console.KeyAvailable)
            {
                keyBuffer[position++] = (char)key;
                keyBuffer[position++] = keyChar;

                if (position >= (KeyBufferSize - 2))
                {
                    if (position >= KeyBufferSize ||
                        char.IsLowSurrogate(keyChar))
                    {
                        flush = true;
                    }
                }
            }
            else
            {
                flush = true;
            }

            if (flush)
            {// Flush
                var result = this.Flush(buffer, cursorPos, keyBuffer, position);
            }
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

        try
        {
            Console.WriteLine(message);
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

    private string? Flush(InputBuffer buffer, (int Left, int Top) cursorPos, Span<char> keyBuffer, int length)
    {
        return null;
    }

    private InputBuffer? FindBuffer(int cursorTop)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.buffers.Count == 0)
            {
                return null;
            }

            if (cursorTop <= this.startingCursorTop)
            {
                return this.buffers[0];
            }

            var y = this.startingCursorTop;
            for (int i = 0; i < this.buffers.Count; i++)
            {
                y += this.buffers[0].GetHeight();
                if (cursorTop < y)
                {
                    return this.buffers[i];
                }
            }

            return this.buffers[0];
        }
    }

    private InputBuffer RentBuffer()
    {
        var buffer = this.bufferPool.Rent();
        buffer.Clear();
        return buffer;
    }

    private void ReturnAllBuffersInternal()
    {
        foreach (var buffer in this.buffers)
        {
            this.bufferPool.Return(buffer);
        }

        this.buffers.Clear();
    }

    /*private InputBuffer? StoreBuffer()
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
    }*/
}
