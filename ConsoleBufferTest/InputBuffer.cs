// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class InputBuffer
{
    public const int BufferSize = 1_024;

    public string? Prompt { get; set; }

    public char[] Array { get; } = new char[BufferSize];

    public int TextLength { get; set; }

    public int PromptLength => this.Prompt?.Length ?? 0;

    public int TotalLength => this.PromptLength + this.TextLength;

    public Span<char> TextSpan => this.Array.AsSpan(0, this.TextLength);

    public void Clear()
    {
        this.Prompt = null;
        this.TextLength = 0;
    }
}
