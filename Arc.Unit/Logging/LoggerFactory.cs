// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class LoggerFactory<TLogSource> : ILogger<TLogSource>
{
    public LoggerFactory(LogScope loggerScope)
    {
        this.loggerScope = loggerScope;
    }

    public ILogWriter? TryGet(LogLevel logLevel = LogLevel.Information)
        => this.loggerScope.GetLogWriter<TLogSource>(logLevel);

    private readonly LogScope loggerScope;
}
