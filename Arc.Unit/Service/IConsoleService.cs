// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface IConsoleService
{
    public void Write(string? message = default);

    public void WriteLine(string? message = default);

    public Task<InputResult> ReadLine(string? prompt = default, CancellationToken cancellationToken = default);

    public ConsoleKeyInfo ReadKey(bool intercept);

    public bool KeyAvailable { get; }
}
