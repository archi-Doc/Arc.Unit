﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface IConsoleService
{
    public void Write(string? message = null);

    public void WriteLine(string? message = null);

    public string? ReadLine();

    public ConsoleKeyInfo ReadKey(bool intercept);

    public bool KeyAvailable { get; }
}
