// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class LoggerFactory<TLogSource> : ILogger<TLogSource>
{
    public LoggerFactory(LoggerScope loggerScope)
    {
        this.loggerScope = loggerScope;
    }

    public ILogWriter? TryGet(LogLevel logLevel = LogLevel.Information)
        => this.loggerScope.TryGet<TLogSource>(logLevel);

    private readonly LoggerScope loggerScope;
}
