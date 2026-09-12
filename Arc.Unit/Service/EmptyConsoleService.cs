// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Discards output and returns successful empty input, or Canceled for an already-canceled token.
/// </summary>
public sealed class EmptyConsole : IConsoleService
{
    private static readonly Task<InputResult> EmptyInput = Task.FromResult(new InputResult(InputResultKind.Success));
    private static readonly Task<InputResult> CanceledInput = Task.FromResult(new InputResult(InputResultKind.Canceled));

    /// <inheritdoc/>
    public bool KeyAvailable => false;

    /// <inheritdoc/>
    public bool EnableColor { get; set; }

    /// <inheritdoc/>
    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        return default;
    }

    /// <inheritdoc/>
    public Task<InputResult> ReadLine(CancellationToken cancellationToken = default)
    {
        return cancellationToken.IsCancellationRequested ? CanceledInput : EmptyInput;
    }

    /// <inheritdoc/>
    public void Write(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor)
    {
    }

    /// <inheritdoc/>
    public void Write(ReadOnlySpan<char> message, ConsoleColor color = ConsoleHelper.DefaultColor)
    {
    }

    /// <inheritdoc/>
    public void WriteLine(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor)
    {
    }

    /// <inheritdoc/>
    public void WriteLine(ReadOnlySpan<char> message, ConsoleColor color = ConsoleHelper.DefaultColor)
    {
    }
}
