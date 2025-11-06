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

    public InputConsole InputConsole { get; }

    public int Left { get; set; }

    public int Top { get; set; }

    public int CursorLeft { get; set; }

    public int CursorTop { get; set; }

    public string? Prompt { get; private set; }

    public int PromtWidth { get; private set; }

    public int Length { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int TotalWidth => this.PromtWidth + this.Width;

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
        var key = keyInfo.Key;
        if (keyInfo.Key != ConsoleKey.None)
        {// Control
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
                        this.UpdateConsole(arrayPosition - 2, this.Length, 0, true);
                    }
                    else
                    {
                        this.RemoveAt(arrayPosition - 1);
                        this.UpdateConsole(arrayPosition - 1, this.Length, 0, true);
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

                    this.UpdateConsole(arrayPosition, this.Length, 0, true);
                }

                return false;
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
        else
        {// Not control
            var arrayPosition = this.GetArrayPosition();
            this.ProcessCharacterInternal(arrayPosition, charBuffer);
        }

        return false;
    }

    /*public int GetWidth()
    {
        return (int)BaseHelper.Sum(this.widthArray.AsSpan(0, this.Length));
    }*/

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
            this.UpdateConsole(arrayPosition, this.Length, width);
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

    private void UpdateConsole(int startIndex, int endIndex, int cursorDif, bool eraseLine = false)
    {
        Debug.Assert(startIndex >= 0 && endIndex <= this.Length && startIndex <= endIndex);

        var length = endIndex - startIndex;
        var charSpan = this.charArray.AsSpan(startIndex, length);
        var widthSpan = this.widthArray.AsSpan(startIndex, length);
        this.InputConsole.Update(charSpan, widthSpan, cursorDif, eraseLine);
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

    private void SetCursorPosition(int cursorLeft, int cursorTop, bool showCursor)
    {
        try
        {
            this.InputConsole.SetCursorPosition(this.Left + cursorLeft, this.Top + cursorTop, showCursor);
            this.CursorLeft = cursorLeft;
            this.CursorTop = cursorTop;
        }
        catch
        {
        }
    }
}
