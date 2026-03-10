// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class LoggerFactory<TLogSource> : ILogger<TLogSource>
{
    public LoggerFactory(LoggerUnit unitLogger)
    {
        this.unitLogger = unitLogger;
    }

    public ILogWriter? TryGet(LogLevel logLevel = LogLevel.Information)
        => this.unitLogger.TryGet<TLogSource>(logLevel);

    private readonly LoggerUnit unitLogger;
}
