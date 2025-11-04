// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Arc.Unit;

public partial class InputConsole : IConsoleService
{
    private const int KeyBufferSize = 16;

    public ConsoleColor DefaultInputColor { get; set; }

    public bool IsInsertMode { get; set; } = false;

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

        while (true)
        {
            ConsoleKeyInfo keyInfo;
            try
            {
                keyInfo = Console.ReadKey(intercept: true);
            }
            catch
            {
                keyInfo = new(default, ConsoleKey.Enter, false, false, false);
            }

            bool flush = true;
            if (IsControl(keyInfo))
            {// Control
            }
            else
            {// Displayable character
                keyBuffer[position++] = keyInfo.KeyChar;
                try
                {
                    if (Console.KeyAvailable)
                    {
                        flush = false;
                        if (position >= (KeyBufferSize - 2))
                        {
                            if (position >= KeyBufferSize ||
                                char.IsLowSurrogate(keyInfo.KeyChar))
                            {
                                flush = true;
                            }
                        }
                    }
                }
                catch
                {
                }

                keyInfo = default;
            }

            if (flush)
            {// Flush
                var result = this.Flush(keyInfo, keyBuffer.Slice(0, position));
                position = 0;
                if (result is not null)
                {
                    return result;
                }
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

    private static bool IsControl(ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.KeyChar == 0)
        {
            return true;
        }
        else if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) ||
            keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt))
        {
            return true;
        }

        return false;
    }

    private string? Flush(Span<char> keyBuffer)
    {
        (var cursorLeft, var cursorTop) = Console.GetCursorPosition();

        using (this.lockObject.EnterScope())
        {
            var buffer = this.FindBuffer(ref cursorLeft, ref cursorTop);
            if (buffer is null)
            {
                return null;
            }

            if (buffer.ProcessInternal(this, cursorLeft, cursorTop, keyBuffer))
            {
                var length = 0;
                for (int i = 0; i < this.buffers.Count; i++)
                {
                    length += this.buffers[i].Width;
                }

                var result = string.Create(length, this.buffers, static (span, buffers) =>
                {
                    var position = 0;
                    for (int i = 0; i < buffers.Count; i++)
                    {
                        var buffer = buffers[i];
                        buffer.TextSpan.CopyTo(span.Slice(position, buffer.Length));
                        position += buffer.Length;
                    }
                });

                this.ReturnAllBuffersInternal();
                return result;
            }
            else
            {
                return null;
            }
        }
    }

    private InputBuffer? FindBuffer(ref int cursorLeft, ref int cursorTop)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.buffers.Count == 0)
            {
                return null;
            }

            /*if (cursorTop <= this.startingCursorTop)
            {
                cursorTop = 0;
                return this.buffers[0];
            }*/

            var y = this.startingCursorTop;
            for (int i = 0; i < this.buffers.Count; i++)
            {
                var prevY = y;
                y += this.buffers[i].GetHeight();
                if (cursorTop < y)
                {
                    cursorTop -= prevY;
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
