// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogContext
{
    ILogWriter? TryGet<TLogOutput>(LogLevel logLevel = LogLevel.Information);

    IConsoleService ConsoleService { get; }
}
