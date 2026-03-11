// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogService
{
    LogWriter? GetLogWriter<TLogOutput>(LogLevel logLevel = LogLevel.Information);

    ILogger<TLogSource> GetLogger<TLogSource>();

    IConsoleService ConsoleService { get; }
}
