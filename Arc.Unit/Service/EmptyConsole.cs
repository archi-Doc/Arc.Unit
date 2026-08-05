// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public sealed class EmptyConsole : IConsoleService
{
    public bool KeyAvailable => false;

    public bool EnableColor { get; set; }

    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        return default;
    }

    public Task<InputResult> ReadLine(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new InputResult(InputResultKind.Success));
    }

    public void Write(string? message = null, ConsoleColor color = (ConsoleColor)(-1))
    {
    }

    public void WriteLine(string? message = null, ConsoleColor color = (ConsoleColor)(-1))
    {
    }

    public void WriteLine(ReadOnlySpan<char> message, ConsoleColor color = (ConsoleColor)(-1))
    {
    }
}
