// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// <see cref="IConsoleService"/> which discards all output and returns an empty input.
/// </summary>
public sealed class EmptyConsole : IConsoleService
{
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
        return Task.FromResult(new InputResult(InputResultKind.Success));
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
