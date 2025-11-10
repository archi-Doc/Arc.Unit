// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public enum InputResultKind
{
    /// <summary>
    /// The input was completed successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The input was canceled by the user (e.g., pressing Esc).
    /// </summary>
    Canceled = 1,

    /// <summary>
    /// The application received a termination request (e.g., Ctrl+C).
    /// </summary>
    Terminated = 2,
}

public readonly struct InputResult
{
    public readonly InputResultKind Kind;

    public string Text => this.text ?? string.Empty;

    private readonly string? text;

    public InputResult(string text)
    {
        this.Kind = InputResultKind.Success;
        this.text = text;
    }

    public InputResult(InputResultKind kind)
    {
        this.Kind = kind;
    }

    public bool IsSuccess => this.Kind == InputResultKind.Success;
}
