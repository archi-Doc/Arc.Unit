// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Represents the result of an input operation, containing either the input text or an indication of why the input was not completed.
/// </summary>
public readonly struct InputResult
{
    /// <summary>
    /// Gets the kind of result, indicating whether the input was successful, canceled, or terminated.
    /// </summary>
    public readonly InputResultKind Kind;

    /// <summary>
    /// Gets the input text. Returns an empty string if no text is available.
    /// </summary>
    public string Text => this.text ?? string.Empty;

    private readonly string? text;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputResult"/> struct with successful input text.
    /// </summary>
    /// <param name="text">The input text that was successfully captured.</param>
    public InputResult(string text)
    {
        this.Kind = InputResultKind.Success;
        this.text = text;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InputResult"/> struct with a specific result kind.
    /// </summary>
    /// <param name="kind">The kind of result indicating why the input operation completed.</param>
    public InputResult(InputResultKind kind)
    {
        this.Kind = kind;
    }

    /// <summary>
    /// Gets a value indicating whether the input operation was successful.
    /// </summary>
    public bool IsSuccess => this.Kind == InputResultKind.Success;
}
