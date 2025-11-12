// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using Arc.Unit;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.InputConsole;

internal class InputBuffer
{
    private const int BufferSize = 1_024;
    private const int BufferMargin = 32;
    private const int MaxPromptWidth = 256;

    public InputConsole InputConsole { get; }

    public int Left { get; set; }

    public int Top { get; set; }

    /// <summary>
    /// Gets or sets the cursor's horizontal position relative to the buffer's left edge.
    /// </summary>
    public int CursorLeft { get; set; }

    /// <summary>
    /// Gets or sets the cursor's vertical position relative to the buffer's top edge.
    /// </summary>
    public int CursorTop { get; set; }

    public string? Prompt { get; private set; }

    public int PromtWidth { get; private set; }

    public int Length { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int TotalWidth => this.PromtWidth + this.Width;

    public int WindowWidth => this.InputConsole.WindowWidth;

    public int WindowHeight => this.InputConsole.WindowHeight;

    public Span<char> TextSpan => this.charArray.AsSpan(0, this.Length);

    private char[] charArray = new char[BufferSize];
    private byte[] widthArray = new byte[BufferSize];

    public InputBuffer(InputConsole inputConsole)
    {
        this.InputConsole = inputConsole;
    }

    public void Clear()
    {
        this.Prompt = default;
        this.PromtWidth = 0;
        this.Length = 0;
        this.Width = 0;
    }

    public bool ProcessInternal(ConsoleKeyInfo keyInfo, Span<char> charBuffer)
    {
        if (charBuffer.Length > 0)
        {
            var arrayPosition = this.GetArrayPosition();
            this.ProcessCharacterInternal(arrayPosition, charBuffer);
        }

        if (keyInfo.Key != ConsoleKey.None)
        {// Control
            var key = keyInfo.Key;
            if (key == ConsoleKey.Enter)
            {// Exit or Multiline """
                return true;
            }
            else if (key == ConsoleKey.Backspace)
            {
                var arrayPosition = this.GetArrayPosition();
                if (arrayPosition > 0)
                {
                    this.MoveLeft(arrayPosition);
                    if (char.IsLowSurrogate(this.charArray[arrayPosition - 1]) &&
                        (arrayPosition > 1) &&
                        char.IsHighSurrogate(this.charArray[arrayPosition - 2]))
                    {
                        this.Remove2At(arrayPosition - 2);
                        this.Write(arrayPosition - 2, this.Length, 0, true);
                    }
                    else
                    {
                        this.RemoveAt(arrayPosition - 1);
                        this.Write(arrayPosition - 1, this.Length, 0, true);
                    }
                }

                return false;
            }
            else if (key == ConsoleKey.Delete)
            {
                var arrayPosition = this.GetArrayPosition();
                if (arrayPosition < this.Length)
                {
                    if (char.IsHighSurrogate(this.charArray[arrayPosition]) &&
                        (arrayPosition + 1) < this.Length &&
                        char.IsLowSurrogate(this.charArray[arrayPosition + 1]))
                    {
                        this.Remove2At(arrayPosition);
                    }
                    else
                    {
                        this.RemoveAt(arrayPosition);
                    }

                    this.Write(arrayPosition, this.Length, 0, true);
                }

                return false;
            }
            else if (key == ConsoleKey.U && keyInfo.Modifiers == ConsoleModifiers.Control)
            {// Ctrl+U: Clear line
                this.ClearLine();
            }
            else if (key == ConsoleKey.Home)
            {
                this.SetCursorPosition(this.PromtWidth, 0, false);
            }
            else if (key == ConsoleKey.End)
            {
                var newCursor = this.ToCursor(this.Width);
                this.SetCursorPosition(newCursor.Left, newCursor.Top, false);
            }
            else if (key == ConsoleKey.LeftArrow)
            {
                var arrayPosition = this.GetArrayPosition();
                this.MoveLeft(arrayPosition);
                return false;
            }
            else if (key == ConsoleKey.RightArrow)
            {
                var arrayPosition = this.GetArrayPosition();
                this.MoveRight(arrayPosition);
                return false;
            }
            else if (key == ConsoleKey.UpArrow ||
                key == ConsoleKey.DownArrow)
            {// History or move line
                return false;
            }
            else if (key == ConsoleKey.Insert)
            {// Toggle insert mode
                // Overtype mode is not implemented yet.
                // this.InputConsole.IsInsertMode = !this.InputConsole.IsInsertMode;
            }
        }

        return false;
    }

    private void ClearLine()
    {
        Array.Fill<char>(this.charArray, ' ', 0, this.Width);
        Array.Fill<byte>(this.widthArray, 1, 0, this.Width);
        this.Length = this.Width;
        this.Write(0, this.Width, 0, false);

        this.Length = 0;
        this.Width = 0;
        this.SetCursorPosition(this.PromtWidth, 0, false);
        // this.UpdateConsole(0, this.Length, 0, true);
    }

    /*public int GetWidth()
    {
        return (int)BaseHelper.Sum(this.widthArray.AsSpan(0, this.Length));
    }*/

    public void SetPrompt(string? prompt)
    {
        if (prompt?.Length > MaxPromptWidth)
        {
            prompt = prompt.Substring(0, MaxPromptWidth);
        }

        this.Prompt = prompt;
        this.PromtWidth = InputConsoleHelper.GetWidth(this.Prompt);
    }

    private void EnsureCapacity(int capacity)
    {
        capacity += BufferMargin;
        if (this.charArray.Length < capacity)
        {
            var newSize = this.charArray.Length;
            while (newSize < capacity)
            {
                newSize *= 2;
            }

            Array.Resize(ref this.charArray, newSize);
            Array.Resize(ref this.widthArray, newSize);
        }
    }

    private void ProcessCharacterInternal(int arrayPosition, Span<char> charBuffer)
    {
        // var bufferWidth = InputConsoleHelper.GetWidth(charBuffer);

        if (this.InputConsole.IsInsertMode)
        {// Insert
            this.EnsureCapacity(this.Length + charBuffer.Length);

            this.charArray.AsSpan(arrayPosition, this.Length - arrayPosition).CopyTo(this.charArray.AsSpan(arrayPosition + charBuffer.Length));
            charBuffer.CopyTo(this.charArray.AsSpan(arrayPosition));
            this.widthArray.AsSpan(arrayPosition, this.Length - arrayPosition).CopyTo(this.widthArray.AsSpan(arrayPosition + charBuffer.Length));
            var width = 0;
            for (var i = 0; i < charBuffer.Length; i++)
            {
                int w;
                var c = charBuffer[i];
                if (char.IsHighSurrogate(c) && (i + 1) < charBuffer.Length && char.IsLowSurrogate(charBuffer[i + 1]))
                {
                    var codePoint = char.ConvertToUtf32(c, charBuffer[i + 1]);
                    w = InputConsoleHelper.GetCharWidth(codePoint);
                    this.widthArray[arrayPosition + i++] = 0;
                    this.widthArray[arrayPosition + i] = (byte)w;
                }
                else
                {
                    w = InputConsoleHelper.GetCharWidth(c);
                    this.widthArray[arrayPosition + i] = (byte)w;
                }

                width += w;
            }

            this.Length += charBuffer.Length;
            this.Width += width;
            this.Write(arrayPosition, this.Length, width);
        }
        else
        {// Overtype (Not implemented yet)
            /*this.EnsureCapacity(arrayPosition + charBuffer.Length);

            charBuffer.CopyTo(this.charArray.AsSpan(arrayPosition));
            for (var i = 0; i < charBuffer.Length; i++)
            {
                var c = charBuffer[i];
                int width, dif;
                if (char.IsHighSurrogate(c) && (i + 1) < charBuffer.Length && char.IsLowSurrogate(charBuffer[i + 1]))
                {
                    var codePoint = char.ConvertToUtf32(c, charBuffer[i + 1]);
                    dif = InputConsoleHelper.GetCharWidth(codePoint);
                    width = dif;
                    dif -= this.widthArray[arrayPosition + i];
                    this.widthArray[arrayPosition + i++] = 0;
                    dif -= this.widthArray[arrayPosition + i];
                    this.widthArray[arrayPosition + i] = (byte)width;
                }
                else
                {
                    dif = InputConsoleHelper.GetCharWidth(c);
                    width = dif;
                    dif -= this.widthArray[arrayPosition + i];
                    this.widthArray[arrayPosition + i] = (byte)width;
                }

                this.Width += dif;
            }

            this.UpdateConsole(arrayPosition, arrayPosition + charBuffer.Length, 0);*/
        }
    }

    private int GetArrayPosition()
    {
        var index = this.GetCursorIndex();

        int arrayPosition;
        for (arrayPosition = 0; arrayPosition < this.Length; arrayPosition++)
        {
            if (index <= 0)
            {
                break;
            }

            index -= this.widthArray[arrayPosition];
        }

        return arrayPosition;
    }

    internal void Write(int startIndex, int endIndex, int cursorDif, bool eraseLine = false)
    {
        var length = startIndex < 0 ? this.Length : endIndex - startIndex;
        var charSpan = this.charArray.AsSpan(startIndex, length);
        var widthSpan = this.widthArray.AsSpan(startIndex, length);
        var totalWidth = startIndex < 0 ? this.TotalWidth : (int)BaseHelper.Sum(widthSpan);
        var startPosition = startIndex < 0 ? 0 : this.PromtWidth + (int)BaseHelper.Sum(this.widthArray.AsSpan(0, startIndex));

        var startCursor = this.Left + (this.Top * this.WindowWidth) + startPosition;
        var windowRemaining = (this.WindowWidth * this.WindowHeight) - startCursor;
        if (totalWidth > windowRemaining)
        {
        }

        var startCursorLeft = startCursor % this.WindowWidth;
        var startCursorTop = startCursor / this.WindowWidth;

        var scroll = startCursorTop + 1 + ((startCursorLeft + totalWidth) / this.WindowWidth) - this.WindowHeight;
        if (scroll > 0)
        {
            this.InputConsole.Scroll(scroll);
        }

        startCursor += cursorDif;
        var newCursorLeft = startCursor % this.WindowWidth;
        var newCursorTop = startCursor / this.WindowWidth;
        var appendLineFeed = startCursor == (this.WindowWidth * this.WindowHeight);

        ReadOnlySpan<char> span;
        var buffer = this.InputConsole.WindowBuffer.AsSpan();
        var written = 0;

        // Hide cursor
        /*span = ConsoleHelper.HideCursorSpan;
        span.CopyTo(buffer);
        written += span.Length;
        buffer = buffer.Slice(span.Length);

        if (startCursorLeft != (this.Left + this.CursorLeft) || startCursorTop != (this.Top + this.CursorTop))
        {// Move cursor
            span = ConsoleHelper.SetCursorSpan;
            span.CopyTo(buffer);
            buffer = buffer.Slice(span.Length);
            written += span.Length;

            var x = newCursorTop + 1;
            var y = newCursorLeft + 1;
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
        }*/

        if (startIndex < 0 && this.Prompt is not null)
        {// Prompt
            span = this.Prompt.AsSpan();
            span.CopyTo(buffer);
            written += span.Length;
            buffer = buffer.Slice(span.Length);
        }

        // Input color
        span = ConsoleHelper.GetForegroundColorEscapeCode(this.InputConsole.InputColor).AsSpan();
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

        // Reset color
        /*span = ConsoleHelper.ResetSpan;
        span.CopyTo(buffer);
        written += span.Length;
        buffer = buffer.Slice(span.Length);*/

        if (eraseLine)
        {// Erase line
            span = ConsoleHelper.EraseLineSpan;
            span.CopyTo(buffer);
            written += span.Length;
            buffer = buffer.Slice(span.Length);
        }

        try
        {
            // Console.Out.Write("X");
            Console.Out.Write(this.InputConsole.WindowBuffer.AsSpan(0, written));
            // this.SetCursorPosition(newCursorLeft - this.Left, newCursorTop - this.Top, true);
        }
        catch
        {
        }
    }

    private void RemoveAt(int index)
    {
        this.Length--;
        this.Width -= this.widthArray[index];
        this.charArray.AsSpan(index + 1, this.Length - index).CopyTo(this.charArray.AsSpan(index));
        this.widthArray.AsSpan(index + 1, this.Length - index).CopyTo(this.widthArray.AsSpan(index));
    }

    private void Remove2At(int index)
    {
        this.Length -= 2;
        var w = this.widthArray[index] + this.widthArray[index + 1];
        this.Width -= w;
        this.charArray.AsSpan(index + 2, this.Length - index).CopyTo(this.charArray.AsSpan(index));
        this.widthArray.AsSpan(index + 2, this.Length - index).CopyTo(this.widthArray.AsSpan(index));
    }

    private int GetLeftWidth(int index)
    {
        if (index < 1)
        {
            return 0;
        }

        if (char.IsLowSurrogate(this.charArray[index - 1]) &&
            index > 1 &&
            char.IsHighSurrogate(this.charArray[index - 2]))
        {
            return this.widthArray[index - 1] + this.widthArray[index - 2];
        }
        else
        {
            return this.widthArray[index - 1];
        }
    }

    private int GetRightWidth(int index)
    {
        if (index >= this.Length)
        {
            return 0;
        }

        if (char.IsHighSurrogate(this.charArray[index]) &&
            (index + 1) < this.Length &&
            char.IsLowSurrogate(this.charArray[index + 1]))
        {
            return this.widthArray[index] + this.widthArray[index + 1];
        }
        else
        {
            return this.widthArray[index];
        }
    }

    private int GetCursorIndex()
    {
        var index = this.CursorLeft - this.PromtWidth + (this.CursorTop * this.InputConsole.WindowWidth);
        return index;
    }

    private (int Left, int Top) ToCursor(int cursorIndex)
    {
        cursorIndex += this.PromtWidth;
        var top = cursorIndex / this.InputConsole.WindowWidth;
        var left = cursorIndex - (top * this.InputConsole.WindowWidth);
        return (left, top);
    }

    private void MoveLeft(int arrayPosition)
    {
        if (arrayPosition == 0)
        {
            return;
        }

        var width = this.GetLeftWidth(arrayPosition);
        var cursorIndex = this.GetCursorIndex() - width;
        if (cursorIndex >= 0)
        {
            var newCursor = this.ToCursor(cursorIndex);
            if (this.CursorLeft != newCursor.Left ||
                this.CursorTop != newCursor.Top)
            {
                this.SetCursorPosition(newCursor.Left, newCursor.Top, false);
            }
        }
    }

    private void MoveRight(int arrayPosition)
    {
        if (arrayPosition >= this.Length)
        {
            return;
        }

        var width = this.GetRightWidth(arrayPosition);
        var cursorIndex = this.GetCursorIndex() + width;
        if (cursorIndex >= 0)
        {
            var newCursor = this.ToCursor(cursorIndex);
            if (this.CursorLeft != newCursor.Left ||
                this.CursorTop != newCursor.Top)
            {
                this.SetCursorPosition(newCursor.Left, newCursor.Top, false);
            }
        }
    }

    /// <summary>
    /// Specifies the cursor position relative to the current InputBuffer’s Left and Top.
    /// </summary>
    private void SetCursorPosition(int cursorLeft, int cursorTop, bool showCursor)
    {
        try
        {
            if (showCursor ||
                cursorLeft != this.CursorLeft ||
                cursorTop != this.CursorTop)
            {
                this.InputConsole.SetCursorPosition(this.Left + cursorLeft, this.Top + cursorTop, showCursor);
                this.CursorLeft = cursorLeft;
                this.CursorTop = cursorTop;
            }
        }
        catch
        {
        }
    }
}
