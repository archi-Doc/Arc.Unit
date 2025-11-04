// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;

namespace Arc.Unit;

internal class InputBuffer
{
    public const int BufferSize = 1_024;

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

    public bool ProcessInternal(InputConsole inputConsole, int cursorLeft, int cursorTop, Span<char> keyBuffer)
    {
        // Cursor position -> Array position
        var arrayPosition = this.CursorPositionToArrayPosition(cursorLeft, cursorTop);

        var span = keyBuffer;
        var startIndex = -1;
        var endIndex = -1;
        for (var i = 0; i < span.Length; i += 2)
        {
            var key = (ConsoleKey)span[i];
            var keyChar = span[i + 1];

            if (key == ConsoleKey.Enter)
            {// Exit or Multiline """
                return true;
            }
            else if (key == ConsoleKey.Backspace)
            {
                if (arrayPosition > 0)
                {
                    RemoveAt(arrayPosition - 1);
                    this.MoveLeft();
                    startIndex = arrayPosition - 1;
                    endIndex = this.Length;
                    UpdateConsole();
                }

                return false;
            }
            else if (key == ConsoleKey.Delete)
            {
                if (arrayPosition < this.Length)
                {
                    RemoveAt(arrayPosition);
                    startIndex = arrayPosition;
                    endIndex = this.Length;
                    UpdateConsole();
                }

                return false;
            }
            else if (key == ConsoleKey.LeftArrow)
            {
                this.MoveLeft();
                return false;
            }
            else if (key == ConsoleKey.RightArrow)
            {
                this.MoveRight();
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
            else
            {// Other characters
                if (inputConsole.IsInsertMode)
                {
                }

                if (char.IsHighSurrogate(keyChar) && (i + 3) < span.Length && char.IsLowSurrogate(span[i + 3]))
                {// Surrogate pair
                    var lowSurrogate = span[i + 3];
                    this.charArray[arrayPosition] = keyChar;
                    this.widthArray[arrayPosition] = 0;
                    arrayPosition++;

                    var codePoint = char.ConvertToUtf32(keyChar, lowSurrogate);
                    var width = InputConsoleHelper.GetCharWidth(codePoint);
                    this.charArray[arrayPosition] = lowSurrogate;
                    this.widthArray[arrayPosition] = width;
                    this.Width += width;
                    arrayPosition++;
                }
                else if (!char.IsLowSurrogate(keyChar))
                {// 
                    var width = InputConsoleHelper.GetCharWidth(keyChar);
                    this.charArray[arrayPosition] = keyChar;
                    this.widthArray[arrayPosition] = width;
                    this.Width += width;
                    arrayPosition++;
                }

                if (arrayPosition > this.Length)
                {
                    this.Length = arrayPosition;

                    if (this.Length >= (this.charArray.Length - 1))
                    {// Expand buffer (leave a margin for surrogate pairs)
                        this.Length *= 2;
                        Array.Resize(ref this.charArray, this.Length);
                        Array.Resize(ref this.widthArray, this.Length);
                    }
                }
            }
        }

        return false;

        void UpdateConsole()
        {
        }

        void RemoveAt(int index)
        {
            this.Length--;
            this.Width -= this.widthArray[index];
            this.charArray.AsSpan(index + 1, this.Length - index).CopyTo(this.charArray.AsSpan(index));
            this.widthArray.AsSpan(index + 1, this.Length - index).CopyTo(this.widthArray.AsSpan(index));
        }

        void AddChar(char key, char lowSurrogate)
        {
        }
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

        var arrayPosition = 0;
        for (var i = 0; i < this.Length; i++)
        {
            if (pos <= 0)
            {
                arrayPosition = i;
                break;
            }

            pos -= this.widthArray[i];
        }

        return arrayPosition;
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
