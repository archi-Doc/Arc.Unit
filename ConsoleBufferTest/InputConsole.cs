// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Arc.Unit;

public partial class InputConsole : IConsoleService
{
    private const int KeyBufferSize = 16;
    private static readonly ConsoleKeyInfo EnterKeyInfo = new(default, ConsoleKey.Enter, false, false, false);

    public ConsoleColor DefaultInputColor { get; set; }

    public bool IsInsertMode { get; set; } = true;

    public int WindowWidth { get; private set; }

    public int WindowHeight { get; private set; }

    public int RelativeLeft { get; private set; }

    public int RelativeTop { get; private set; }

    private readonly ObjectPool<InputBuffer> bufferPool;

    private readonly Lock lockObject = new();
    private int startingCursorTop;
    private List<InputBuffer> buffers = new();

    public InputConsole(ConsoleColor inputColor = (ConsoleColor)(-1))
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        this.bufferPool = new(() => new InputBuffer(this), 32);
        this.DefaultInputColor = inputColor;
    }

    public string? ReadLine(string? prompt = default)
    {
        InputBuffer? buffer;
        Span<char> charBuffer = stackalloc char[KeyBufferSize];
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
                keyInfo = EnterKeyInfo;
            }

            if (keyInfo.KeyChar == '\n' ||
                keyInfo.Key == ConsoleKey.Enter)
            {
                keyInfo = EnterKeyInfo;
            }
            else if (keyInfo.KeyChar == '\r')
            {// CrLf -> Lf
                continue;
            }

            bool flush = true;
            if (IsControl(keyInfo))
            {// Control
            }
            else
            {// Not control
                charBuffer[position++] = keyInfo.KeyChar;
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
                var result = this.Flush(keyInfo, charBuffer.Slice(0, position));
                position = 0;
                if (result is not null)
                {
                    Console.Out.WriteLine();
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
        else if (keyInfo.Key == ConsoleKey.Enter ||
            keyInfo.Key == ConsoleKey.Backspace)
        {
            return true;
        }

        return false;
    }

    private void Prepare()
    {
        this.RelativeLeft = 0;
        this.RelativeTop = 0;
        this.WindowWidth = 120;
        this.WindowHeight = 30;

        try
        {
            (this.RelativeLeft, this.RelativeTop) = Console.GetCursorPosition();
            this.WindowWidth = Console.WindowWidth;
            this.WindowHeight = Console.WindowHeight;
        }
        catch
        {
        }

        if (this.WindowWidth <= 0)
        {
            this.WindowWidth = 1;
        }

        if (this.WindowHeight <= 0)
        {
            this.WindowHeight = 1;
        }

        if (this.RelativeLeft < 0)
        {
            this.RelativeLeft = 0;
        }
        else if (this.RelativeLeft >= this.WindowWidth)
        {
            this.RelativeLeft = this.WindowWidth - 1;
        }

        if (this.RelativeTop < 0)
        {
            this.RelativeTop = 0;
        }
        else if (this.RelativeTop >= this.WindowHeight)
        {
            this.RelativeTop = this.WindowHeight - 1;
        }
    }

    private string? Flush(ConsoleKeyInfo keyInfo, Span<char> charBuffer)
    {
        this.Prepare();
        using (this.lockObject.EnterScope())
        {
            var buffer = this.PrepareAndFindBuffer();
            if (buffer is null)
            {
                return string.Empty;
            }

            if (buffer.ProcessInternal(keyInfo, charBuffer))
            {// Exit input mode and return the concatenated string.
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

    private InputBuffer? PrepareAndFindBuffer()
    {
        using (this.lockObject.EnterScope())
        {
            if (this.buffers.Count == 0)
            {
                return null;
            }

            // Calculate buffer heights.
            var y = this.RelativeTop;
            InputBuffer? buffer = null;
            foreach (var x in this.buffers)
            {
                x.Left = 0;
                x.Top = y;
                x.Height = (x.Width + this.WindowWidth) / this.WindowWidth;
                y += x.Height;
                if (buffer is null &&
                    this.RelativeTop >= x.Top &&
                    this.RelativeTop < y)
                {
                    buffer = x;
                    this.RelativeTop -= x.Top;
                }
            }

            if (buffer is null)
            {
                buffer = this.buffers[0];
                this.RelativeTop = 0;
            }

            return buffer;
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
}
