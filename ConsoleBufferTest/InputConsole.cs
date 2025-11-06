// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Arc.Unit;

public partial class InputConsole : IConsoleService
{
    private const int CharBufferSize = 1024;
    private const int WindowBufferMargin = 256;
    private static readonly ConsoleKeyInfo EnterKeyInfo = new(default, ConsoleKey.Enter, false, false, false);

    public ConsoleColor InputColor { get; set; } = ConsoleColor.Yellow;

    public bool IsInsertMode { get; set; } = true;

    internal int WindowWidth { get; private set; }

    internal int WindowHeight { get; private set; }

    internal int CursorLeft { get; private set; }

    internal int CursorTop { get; private set; }

    private int WindowBufferCapacity => (this.WindowWidth * this.WindowHeight * 2) + WindowBufferMargin;

    private readonly ObjectPool<InputBuffer> bufferPool;

    private readonly Lock lockObject = new();
    private int startingCursorTop;
    private List<InputBuffer> buffers = new();
    private char[] windowBuffer = [];

    public InputConsole(ConsoleColor inputColor = (ConsoleColor)(-1))
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        this.bufferPool = new(() => new InputBuffer(this), 32);
        if (inputColor >= 0)
        {
            this.InputColor = inputColor;
        }
    }

    public string? ReadLine(string? prompt = default)
    {
        InputBuffer? buffer;
        Span<char> charBuffer = stackalloc char[CharBufferSize];
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
                        if (position >= (CharBufferSize - 2))
                        {
                            if (position >= CharBufferSize ||
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

    internal void Update(ReadOnlySpan<char> charSpan, ReadOnlySpan<byte> widthSpan, int cursorDif, bool eraseLine)
    {
        /*if (eraseLine)
        {
            span = this.charArray.AsSpan(startIndex, length + EraseLineString.Length);
            EraseLineString.CopyTo(span.Slice(length));
            length += EraseLineString.Length;
        }*/

        var cursorIndex = this.CursorLeft + (this.CursorTop * this.WindowWidth);
        var windowRemaining = (this.WindowWidth * this.WindowHeight) - cursorIndex;
        var widthSum = (int)BaseHelper.Sum(widthSpan);
        if (widthSum > windowRemaining)
        {
        }

        ReadOnlySpan<char> span;
        var buffer = this.windowBuffer.AsSpan();
        var written = 0;

        // Hide cursor
        span = ConsoleHelper.HideCursorSpan;
        span.CopyTo(buffer);
        written += span.Length;
        buffer = buffer.Slice(span.Length);

        /*if (cursorDif == 0)
        {// Save cursor
            span = ConsoleHelper.SaveCursorSpan;
            span.CopyTo(buffer);
            written += span.Length;
            buffer = buffer.Slice(span.Length);
        }*/

        // Input color
        span = ConsoleHelper.GetForegroundColorEscapeCode(this.InputColor).AsSpan();
        span.CopyTo(buffer);
        written += span.Length;
        buffer = buffer.Slice(span.Length);

        // Characters
        span = charSpan;
        span.CopyTo(buffer);
        written += span.Length;
        buffer = buffer.Slice(span.Length);

        // Reset
        span = ConsoleHelper.ResetSpan;
        span.CopyTo(buffer);
        written += span.Length;
        buffer = buffer.Slice(span.Length);

        /*if (cursorDif == 0)
        {// Restore cursor
            span = ConsoleHelper.RestoreCursorSpan;
            span.CopyTo(buffer);
            written += span.Length;
            buffer = buffer.Slice(span.Length);
        }*/

        // Show cursor
        /*span = ConsoleHelper.ShowCursorSpan;
        span.CopyTo(buffer);
        written += span.Length;
        buffer = buffer.Slice(span.Length);*/

        try
        {
            Console.Out.Write(this.windowBuffer.AsSpan(0, written));

            var cursorLeft = 0;
            if (cursorDif != int.MinValue)
            {
                cursorLeft = this.CursorLeft + cursorDif;
            }

            if (cursorDif != int.MinValue)
            {
                var windowWidth = this.WindowWidth;
                if (cursorLeft >= 0 && cursorLeft < windowWidth)
                {
                    Console.CursorLeft = cursorLeft;
                }
                else
                {
                    var cursorTop = Console.CursorTop;
                    int y;
                    if (cursorLeft >= 0)
                    {
                        y = cursorLeft / windowWidth;
                    }
                    else
                    {
                        y = (cursorLeft / windowWidth) - 1;
                    }

                    cursorTop += y;
                    cursorLeft -= windowWidth * y;

                    if (cursorTop >= 0 && cursorTop < Console.WindowHeight)
                    {
                        Console.SetCursorPosition(cursorLeft, cursorTop);
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
            Console.CursorVisible = true;
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
        this.CursorLeft = 0;
        this.CursorTop = 0;
        this.WindowWidth = 120;
        this.WindowHeight = 30;

        try
        {
            (this.CursorLeft, this.CursorTop) = Console.GetCursorPosition();
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

        if (this.CursorLeft < 0)
        {
            this.CursorLeft = 0;
        }
        else if (this.CursorLeft >= this.WindowWidth)
        {
            this.CursorLeft = this.WindowWidth - 1;
        }

        if (this.CursorTop < 0)
        {
            this.CursorTop = 0;
        }
        else if (this.CursorTop >= this.WindowHeight)
        {
            this.CursorTop = this.WindowHeight - 1;
        }

        if (this.windowBuffer.Length != this.WindowBufferCapacity)
        {
            this.windowBuffer = new char[this.WindowBufferCapacity];
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
            var y = this.startingCursorTop;
            InputBuffer? buffer = null;
            foreach (var x in this.buffers)
            {
                x.Left = 0;
                x.Top = y;
                x.Height = (x.TotalWidth + this.WindowWidth) / this.WindowWidth;
                y += x.Height;
                if (buffer is null &&
                    this.CursorTop >= x.Top &&
                    this.CursorTop < y)
                {
                    x.CursorLeft = this.CursorLeft - x.Left;
                    x.CursorTop = this.CursorTop - x.Top;
                    buffer = x;
                }
                else
                {
                    x.CursorTop = 0;
                }
            }

            buffer ??= this.buffers[0];
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
