// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Collections;
using Arc.Threading;
using Arc.Unit;

namespace Arc.InputConsole;

public partial class InputConsole : IConsoleService
{
    private const int CharBufferSize = 1024;
    private const int WindowBufferMargin = 512;
    private static readonly ConsoleKeyInfo EnterKeyInfo = new(default, ConsoleKey.Enter, false, false, false);
    private static readonly ConsoleKeyInfo SpaceKeyInfo = new(' ', ConsoleKey.Spacebar, false, false, false);

    public ILogger? Logger { get; set; }

    public ConsoleColor InputColor { get; set; } = ConsoleColor.Yellow;

    public bool IsInsertMode { get; set; } = true;

    internal ConsoleKeyReader Reader { get; private set; } = new();

    internal int WindowWidth { get; private set; }

    internal int WindowHeight { get; private set; }

    internal int CursorLeft { get; set; }

    internal int CursorTop { get; set; }

    internal int StartingCursorTop { get; set; }

    internal char[] WindowBuffer => this.windowBuffer;

    private int WindowBufferCapacity => (this.WindowWidth * this.WindowHeight * 2) + WindowBufferMargin;

    private readonly ObjectPool<InputBuffer> bufferPool;

    private readonly Lock lockObject = new();
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

    public InputResult ReadLine(string? prompt = default)
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
            this.StartingCursorTop = Console.CursorTop;
        }

        if (!string.IsNullOrEmpty(prompt))
        {
            Console.Out.Write(prompt);
        }

        // Console.TreatControlCAsInput = true;
        ConsoleKeyInfo pendingKeyInfo = default;
        while (!ThreadCore.Root.IsTerminated)
        {
            // this.CheckResize();

            // Polling isn’t an ideal approach, but due to the fact that the normal method causes a significant performance drop and that the function must be able to exit when the application terminates, this implementation was chosen.

            /*if (!this.reader.TryRead(out var keyInfo))
            {
                Thread.Sleep(10);
                continue;
            }*/

            if (!this.Reader.TryRead(out var keyInfo))
            {
                Thread.Sleep(10);
                continue;
            }

/*ConsoleKeyInfo keyInfo;
try
{
    if (!Console.KeyAvailable)
    {
        Thread.Sleep(10);
        continue;
    }

    keyInfo = Console.ReadKey(intercept: true);
}
catch
{
    return new(InputResultKind.Terminated);
}*/

ProcessKeyInfo:
            if (keyInfo.KeyChar == '\n' ||
                keyInfo.Key == ConsoleKey.Enter)
            {
                keyInfo = EnterKeyInfo;
            }
            else if (keyInfo.KeyChar == '\t' ||
                keyInfo.Key == ConsoleKey.Tab)
            {// Tab -> Space
                keyInfo = SpaceKeyInfo;
            }
            else if (keyInfo.KeyChar == '\r')
            {// CrLf -> Lf
                continue;
            }

            /*else if (keyInfo.Key == ConsoleKey.C &&
                keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            { // Ctrl+C
                ThreadCore.Root.Terminate(); // Send a termination signal to the root.
                return null;
            }*/

            bool flush = true;
            if (IsControl(keyInfo))
            {// Control
            }
            else
            {// Not control
                charBuffer[position++] = keyInfo.KeyChar;
                if (this.Reader.TryRead(out keyInfo))
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

                    if (flush)
                    {
                        pendingKeyInfo = keyInfo;
                    }
                    else
                    {
                        goto ProcessKeyInfo;
                    }
                }
            }

            if (flush)
            {// Flush
                var result = this.Flush(keyInfo, charBuffer.Slice(0, position));
                position = 0;
                if (result is not null)
                {
                    Console.Out.WriteLine();
                    return new(result);
                }

                if (pendingKeyInfo.Key != ConsoleKey.None)
                {// Process pending key input.
                    keyInfo = pendingKeyInfo;
                    goto ProcessKeyInfo;
                }
            }
        }

        // Terminated
        // this.SetCursorPosition(this.WindowWidth - 1, this.CursorTop, true);
        Console.Out.WriteLine();
        return new(InputResultKind.Terminated);
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

    internal void SetCursorPosition(int cursorLeft, int cursorTop, bool showCursor)
    {// Move and show cursor.
        /*if (this.CursorLeft == cursorLeft &&
            this.CursorTop == cursorTop)
        {
            return;
        }*/

        var buffer = this.windowBuffer.AsSpan();
        var written = 0;

        var span = ConsoleHelper.SetCursorSpan;
        span.CopyTo(buffer);
        buffer = buffer.Slice(span.Length);
        written += span.Length;

        var x = cursorTop + 1;
        var y = cursorLeft + 1;
        x.TryFormat(buffer, out var w);
        buffer = buffer.Slice(w);
        written += w;
        buffer[0] = ';';
        buffer = buffer.Slice(1);
        written += 1;
        y.TryFormat(buffer, out w);
        buffer = buffer.Slice(w);
        written += w;
        buffer[0] = 'H';
        buffer = buffer.Slice(1);
        written += 1;

        if (showCursor)
        {
            span = ConsoleHelper.ShowCursorSpan;
            span.CopyTo(buffer);
            buffer = buffer.Slice(span.Length);
            written += span.Length;
        }

        try
        {
            span = this.windowBuffer.AsSpan(0, written);
            this.Reader.WriteRaw(Encoding.UTF8.GetBytes(span.ToString()));
            // Console.Out.Write(span);
            Console.Error.WriteLine(span.ToString());
        }
        catch
        {
        }

        this.CursorLeft = cursorLeft;
        this.CursorTop = cursorTop;
    }

    internal void Scroll(int scroll)
    {
        if (scroll > 0)
        {
            this.StartingCursorTop -= scroll;
            this.CursorTop -= scroll;
            foreach (var x in this.buffers)
            {
                x.Top -= scroll;
                x.CursorTop += scroll;
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

        var scrolled = this.CursorTop + 1 + ((this.CursorLeft + widthSum) / this.WindowWidth) - this.WindowHeight;
        if (scrolled > 0)
        {
            this.StartingCursorTop -= scrolled;
            this.CursorTop -= scrolled;
            foreach (var x in this.buffers)
            {
                x.Top -= scrolled;
                x.CursorTop += scrolled;
            }
        }

        cursorIndex += cursorDif;
        var newCursorLeft = cursorIndex % this.WindowWidth;
        var newCursorTop = cursorIndex / this.WindowWidth;
        var appendLineFeed = cursorIndex == (this.WindowWidth * this.WindowHeight);

        ReadOnlySpan<char> span;
        var buffer = this.windowBuffer.AsSpan();
        var written = 0;

        // Hide cursor
        span = ConsoleHelper.HideCursorSpan;
        span.CopyTo(buffer);
        written += span.Length;
        buffer = buffer.Slice(span.Length);

        if (true)
        {// Move cursor
            span = ConsoleHelper.SetCursorSpan;
            span.CopyTo(buffer);
            buffer = buffer.Slice(span.Length);
            written += span.Length;

            var x = this.CursorTop + 1;
            var y = this.CursorLeft + 1;
            x.TryFormat(buffer, out var w);
            buffer = buffer.Slice(w);
            written += w;
            buffer[0] = ';';
            buffer = buffer.Slice(1);
            written += 1;
            y.TryFormat(buffer, out w);
            buffer = buffer.Slice(w);
            written += w;
            buffer[0] = 'H';
            buffer = buffer.Slice(1);
            written += 1;
        }

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

        if (appendLineFeed)
        {
            buffer[0] = '\n';
            written += 1;
            buffer = buffer.Slice(1);
        }

        // Reset
        span = ConsoleHelper.ResetSpan;
        span.CopyTo(buffer);
        written += span.Length;
        buffer = buffer.Slice(span.Length);

        if (eraseLine)
        {// Erase line
            span = ConsoleHelper.EraseLineSpan;
            span.CopyTo(buffer);
            written += span.Length;
            buffer = buffer.Slice(span.Length);
        }

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
            // this.Logger?.TryGet()?.Log("Update ->");

            Console.Out.Write(this.windowBuffer.AsSpan(0, written));
            this.SetCursorPosition(newCursorLeft, newCursorTop, true);
            // Console.SetCursorPosition(newCursorLeft, newCursorTop);

            // this.Logger?.TryGet()?.Log("-> Update");
        }
        catch
        {
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
            keyInfo.Key == ConsoleKey.Backspace ||
            keyInfo.Key == ConsoleKey.Escape)
        {
            return true;
        }
        else if (keyInfo.Key == ConsoleKey.Tab)
        {
            return true;
        }

        return false;
    }

    private void CheckResize()
    {//
        var windowWidth = Console.WindowWidth;
        var windowHeight = Console.WindowHeight;
        if (windowWidth == this.WindowWidth &&
            windowHeight == this.WindowHeight)
        {
            return;
        }

        this.Prepare();
        using (this.lockObject.EnterScope())
        {
            this.PrepareAndFindBuffer();
        }
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
                    length += this.buffers[i].Length;
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
            var y = this.StartingCursorTop;
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
