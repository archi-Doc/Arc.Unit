// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class InputBuffer
{
    public const int BufferSize = 1_024;

    public string? Prompt { get; private set; }

    public int PromtWidth { get; private set; }

    public char[] Array { get; } = new char[BufferSize];

    public int TextLength { get; set; }

    public int TextWidth { get; set; }

    public Span<char> TextSpan => this.Array.AsSpan(0, this.TextLength);

    public int MinCursorLeft => this.PromtWidth;

    public int TotalWidth => this.PromtWidth + this.TextWidth;

    // public int MaxCursorLeft => this.PromtWidth + this.TextLength;

    public InputBuffer()
    {
    }

    public int GetHeight()
    {
        try
        {
            var w = Console.WindowWidth;
            return (this.TotalWidth + w - 1) / w;
        }
        catch
        {
            return 1;
        }
    }

    public void SetPrompt(string? prompt)
    {
        this.Prompt = prompt;
        this.PromtWidth = InputConsoleHelper.GetWidth(this.Prompt);
    }

    public void Clear()
    {
        this.Prompt = default;
        this.PromtWidth = 0;
        this.TextLength = 0;
    }
}
