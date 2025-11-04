// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.Unit;

internal class InputBuffer
{
    private const int BufferSize = 1_024;
    private const int BufferMargin = 32;

    private static ReadOnlySpan<char> EraseLineString => "\u001b[K";

    private static ReadOnlySpan<char> EraseLine2String => "\u001b[2K";

    private static ReadOnlySpan<char> ResetString => "\u001b[0m";

    private static ReadOnlySpan<char> SaveCursorString => "\x1b[s";

    private static ReadOnlySpan<char> RestoreCursorString => "\x1b[u";

    public string? Prompt { get; private set; }

    public int PromtWidth { get; private set; }

    public int Length { get; set; }

    public int Width { get; set; }

    public int TotalWidth => this.PromtWidth + this.Width;

    public Span<char> TextSpan => this.charArray.AsSpan(0, this.Length);

    private char[] charArray = new char[BufferSize];

    private byte[] widthArray = new byte[BufferSize];

    public InputBuffer()
    {
    }

    public void Clear()
    {
        this.Prompt = default;
        this.PromtWidth = 0;
        this.Length = 0;
        this.Width = 0;
    }

    public bool ProcessInternal(InputConsole inputConsole, int cursorLeft, int cursorTop, ConsoleKeyInfo keyInfo, Span<char> charBuffer)
    {
        var key = keyInfo.Key;
        if (keyInfo.Key != ConsoleKey.None)
        {// Control
            if (key == ConsoleKey.Enter)
            {// Exit or Multiline """
                return true;
            }
            else if (key == ConsoleKey.Backspace)
            {
                var arrayPosition = this.CursorPositionToArrayPosition(cursorLeft, cursorTop);
                if (arrayPosition > 0)
                {
                    this.RemoveAt(arrayPosition - 1);
                    this.MoveLeft();
                    this.UpdateConsole(arrayPosition - 1, this.Length, 0, true);
                }

                return false;
            }
            else if (key == ConsoleKey.Delete)
            {
                var arrayPosition = this.CursorPositionToArrayPosition(cursorLeft, cursorTop);
                if (arrayPosition < this.Length)
                {
                    this.RemoveAt(arrayPosition);
                    this.UpdateConsole(arrayPosition, this.Length, 0, true);
                }

                return false;
            }
            else if (key == ConsoleKey.LeftArrow)
            {
                this.MoveLeft(cursorLeft, cursorTop);
                return false;
            }
            else if (key == ConsoleKey.RightArrow)
            {
                this.MoveRight(cursorLeft, cursorTop);
                return false;
            }
            else if (key == ConsoleKey.UpArrow ||
                key == ConsoleKey.DownArrow)
            {// History or move line
                return false;
            }
            else if (key == ConsoleKey.Insert)
            {// Toggle insert mode
                inputConsole.IsInsertMode = !inputConsole.IsInsertMode;
            }
        }
        else
        {// Not control
            var arrayPosition = this.CursorPositionToArrayPosition(cursorLeft, cursorTop);
            this.ProcessCharacterInternal(inputConsole, arrayPosition, charBuffer);
        }

        return false;
    }

    public int GetHeight()
    {
        try
        {
            var w = Console.WindowWidth;
            return (this.Width + w - 1) / w;
        }
        catch
        {
            return 1;
        }
    }

    public int GetWidth()
    {
        return (int)BaseHelper.Sum(this.widthArray.AsSpan(0, this.Length));

        /*var width = 0;
        var span = this.widthArray.AsSpan(0, this.Length);
        foreach (var x in span)
        {
            width += x;
        }

        return width;*/
    }

    public void SetPrompt(string? prompt)
    {
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

    private void ProcessCharacterInternal(InputConsole inputConsole, int arrayPosition, Span<char> charBuffer)
    {
        // var bufferWidth = InputConsoleHelper.GetWidth(charBuffer);

        if (inputConsole.IsInsertMode)
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
            this.UpdateConsole(arrayPosition, this.Length, width);
        }
        else
        {// Overtype
            this.EnsureCapacity(arrayPosition + charBuffer.Length);

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

            this.Length += charBuffer.Length;
            this.UpdateConsole(arrayPosition, arrayPosition + charBuffer.Length, int.MinValue);
        }
    }

    private int CursorPositionToArrayPosition(int cursorLeft, int cursorTop)
    {
        var pos = cursorLeft;
        if (cursorTop != 0)
        {
            try
            {
                pos += cursorTop * Console.WindowWidth;
            }
            catch
            {
            }
        }

        pos -= this.PromtWidth;
        if (pos < 0)
        {
            pos = 0;
        }

        int arrayPosition;
        for (arrayPosition = 0; arrayPosition < this.Length; arrayPosition++)
        {
            if (pos <= 0)
            {
                break;
            }

            pos -= this.widthArray[arrayPosition];
        }

        return arrayPosition;
    }

    private void UpdateConsole(int startIndex, int endIndex, int cursorDif, bool eraseLine = false)
    {
        Debug.Assert(startIndex >= 0 && endIndex <= this.Length && startIndex <= endIndex);

        var length = endIndex - startIndex;
        Span<char> span;
        if (eraseLine)
        {
            span = this.charArray.AsSpan(startIndex, length + EraseLineString.Length);
            EraseLineString.CopyTo(span.Slice(length));
            length += EraseLineString.Length;
        }
        else
        {
            span = this.charArray.AsSpan(startIndex, length);
        }

        // var width = BaseHelper.Sum(this.widthArray.AsSpan(startIndex, length));

        try
        {
            var cursorLeft = 0;
            if (cursorDif != int.MinValue)
            {
                cursorLeft = Console.CursorLeft + cursorDif;
            }

            // var st = $"{SaveCursorString}{span.ToString()}{RestoreCursorString}";
            //Console.Out.Write(st);
            Console.Out.Write("\x1b[?25l");
            Console.Out.Write(span);
            Console.Out.Write("\x1b[?25h");
            if (cursorDif != int.MinValue)
            {
                Console.CursorLeft = cursorLeft;
            }
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

    private void MoveLeft(int cursorLeft, int cursorTop)
    {
        var arrayPosition = this.CursorPositionToArrayPosition(cursorLeft, cursorTop);
        if (arrayPosition == 0)
        {
            return;
        }

        var width = this.widthArray[arrayPosition - 1];//
        try
        {
            var left = Console.CursorLeft;
            if (left > this.PromtWidth)
            {
                Console.CursorLeft = left - width;
            }
        }
        catch
        {
        }
    }

    private void MoveRight(int cursorLeft, int cursorTop)
    {
        var arrayPosition = this.CursorPositionToArrayPosition(cursorLeft, cursorTop);
        try
        {
            var left = Console.CursorLeft;
            if (left < this.TotalWidth)
            {
                Console.CursorLeft = left + 1;
            }
        }
        catch
        {
        }
    }

    private void MoveLeft()
    {
        try
        {
            var left = Console.CursorLeft;
            if (left > this.PromtWidth)
            {
                Console.CursorLeft = left - 1;
            }
        }
        catch
        {
        }
    }

    private void MoveRight()
    {
        try
        {
            var left = Console.CursorLeft;
            if (left < this.TotalWidth)
            {
                Console.CursorLeft = left + 1;
            }
        }
        catch
        {
        }
    }
}
