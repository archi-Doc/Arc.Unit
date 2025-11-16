// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
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

    internal RawConsole RawConsole { get; private set; }

    internal int WindowWidth { get; private set; }

    internal int WindowHeight { get; private set; }

    internal int CursorLeft { get; set; }

    internal int CursorTop { get; set; }

    internal int StartingCursorTop { get; set; }

    internal bool MultilineMode { get; set; }

    internal char[] WindowBuffer => this.windowBuffer;

    internal byte[] Utf8Buffer => this.utf8Buffer;

    private int WindowBufferCapacity => (this.WindowWidth * this.WindowHeight * 2) + WindowBufferMargin;

    private readonly ObjectPool<InputBuffer> bufferPool;

    private readonly Lock lockObject = new();
    private List<InputBuffer> buffers = new();
    private char[] windowBuffer = [];
    private byte[] utf8Buffer = [];

    public InputConsole(ConsoleColor inputColor = (ConsoleColor)(-1))
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        this.RawConsole = new(this);
        this.bufferPool = new(() => new InputBuffer(this), 32);
        if (inputColor >= 0)
        {
            this.InputColor = inputColor;
        }
    }

    public InputResult ReadLine(string? prompt)
        => this.ReadLine(prompt);

    public InputResult ReadLine(string? prompt = default, string? multilinePrompt = default)
    {
        InputBuffer? buffer;
        Span<char> charBuffer = stackalloc char[CharBufferSize];
        var position = 0;

        using (this.lockObject.EnterScope())
        {
            this.ReturnAllBuffersInternal();
            buffer = this.RentBuffer();
            buffer.Initialize(prompt);
            this.buffers.Add(buffer);
            this.StartingCursorTop = Console.CursorTop;
        }

        if (!string.IsNullOrEmpty(prompt))
        {
            Console.Out.Write(prompt);
        }

        (this.CursorLeft, this.CursorTop) = Console.GetCursorPosition();

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

            if (!this.RawConsole.TryRead(out var keyInfo))
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
                if (this.RawConsole.TryRead(out keyInfo))
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
                var result = this.Flush(keyInfo, charBuffer.Slice(0, position), multilinePrompt);
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
        ReadOnlySpan<char> span;

        span = ConsoleHelper.SetCursorSpan;
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
            this.RawConsole.WriteInternal(this.windowBuffer.AsSpan(0, written));
            // Console.Out.Write(this.windowBuffer.AsSpan(0, written));
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
        this.WindowWidth = 120;
        this.WindowHeight = 30;

        try
        {
            this.WindowWidth = Console.WindowWidth;
            this.WindowHeight = Console.WindowHeight;
            // (this.CursorLeft, this.CursorTop) = Console.GetCursorPosition();
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
            this.utf8Buffer = new byte[this.WindowBufferCapacity * 3];
        }
    }

    private string? Flush(ConsoleKeyInfo keyInfo, Span<char> charBuffer, string? multilinePrompt)
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
                if (this.buffers.Count == 0)
                {
                    return string.Empty;
                }

                if (multilinePrompt is not null &&
                    (SimpleCommandLine.SimpleParserHelper.CountTripleQuotes(buffer.TextSpan) % 2) > 0)
                {// Multiple line
                    if (buffer == this.buffers[0])
                    {// Start
                        this.MultilineMode = true;
                    }
                    else
                    {// End
                        this.MultilineMode = false;
                    }
                }

                if (this.MultilineMode)
                {
                    buffer = this.RentBuffer();
                    buffer.Initialize(multilinePrompt);
                    this.buffers.Add(buffer);
                    Console.Out.WriteLine();
                    Console.Out.Write(multilinePrompt);
                    (this.CursorLeft, this.CursorTop) = Console.GetCursorPosition();
                    return null;
                }

                var length = this.buffers[0].Length;
                for (var i = 1; i < this.buffers.Count; i++)
                {
                    length += 1 + this.buffers[i].Length;
                }

                var result = string.Create(length, this.buffers, static (span, buffers) =>
                {
                    buffers[0].TextSpan.CopyTo(span);
                    span = span.Slice(buffers[0].Length);
                    for (var i = 1; i < buffers.Count; i++)
                    {
                        span[0] = '\n';
                        span = span.Slice(1);

                        buffers[i].TextSpan.CopyTo(span);
                        span = span.Slice(buffers[i].Length);
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
