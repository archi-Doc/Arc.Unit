// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Linq;

namespace Arc.Unit;

internal class InputBuffer
{
    public const int BufferSize = 1_024;

    public string? Prompt { get; private set; }

    public int PromtWidth { get; private set; }

    public int Length { get; set; }

    public int Width { get; set; }

    public Span<char> TextSpan => this.charArray.AsSpan(0, this.Length);

    private char[] charArray = new char[BufferSize];

    private sbyte[] widthArray = new sbyte[BufferSize];

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
        var pos = cursorLeft;
        if (cursorTop != 0)
        {
            pos += cursorTop * Console.WindowWidth;
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

        var dif = 0;
        var span = keyBuffer;
        for (var i = 0; i < span.Length; i += 2)
        {
            var key = (ConsoleKey)span[i];
            var keyChar = span[i + 1];

            if (key == ConsoleKey.Enter)
            {
                return true;
            }
            else if (key == ConsoleKey.Backspace)
            {
            }
            else if (key == ConsoleKey.Delete)
            {
            }
            else if (key == ConsoleKey.LeftArrow)
            {
            }
            else if (key == ConsoleKey.RightArrow)
            {
            }
            else if (key == ConsoleKey.UpArrow)
            {
            }
            else if (key == ConsoleKey.DownArrow)
            {
            }
            else
            {
                if (char.IsHighSurrogate(keyChar) && (i + 3) < span.Length && char.IsLowSurrogate(span[i + 3]))
                {// Surrogate pair
                    var lowSurrogate = span[i + 3];
                    this.charArray[arrayPosition] = keyChar;
                    this.widthArray[arrayPosition] = 0;
                    arrayPosition++;

                    var codePoint = char.ConvertToUtf32(keyChar, lowSurrogate);
                    var width = (sbyte)InputConsoleHelper.GetCharWidth(codePoint);
                    this.charArray[arrayPosition] = lowSurrogate;
                    this.widthArray[arrayPosition] = width;
                    this.Width += width;
                    dif += this.widthArray[arrayPosition];
                    arrayPosition++;
                }
                else if (!char.IsLowSurrogate(keyChar))
                {
                    var width = (sbyte)InputConsoleHelper.GetCharWidth(keyChar);
                    this.charArray[arrayPosition] = keyChar;
                    this.widthArray[arrayPosition] = width;
                    this.Width += width;
                    dif += this.widthArray[arrayPosition];
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
    }

    public int GetHeight()
    {
        try
        {
            var w = Console.WindowWidth;
            return (this.GetWidth() + w - 1) / w;
        }
        catch
        {
            return 1;
        }
    }

    public int GetWidth()
    {
        var width = 0;
        var span = this.widthArray.AsSpan(0, this.Length);
        foreach (var x in span)
        {
            width += x;
        }

        return width;
    }

    public void SetPrompt(string? prompt)
    {
        this.Prompt = prompt;
        this.PromtWidth = InputConsoleHelper.GetWidth(this.Prompt);
    }

    private void SetChar(int position, char keyChar, sbyte width)
    {
        this.charArray[position] = keyChar;
        this.widthArray[position] = width;
    }
}
