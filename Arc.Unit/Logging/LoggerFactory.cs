// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class LoggerFactory<TLogSource> : ILogger<TLogSource>
{
    public LoggerFactory(ILogService logService)
    {
        this.logService = logService;
    }

    public LogWriter? TryGet(LogLevel logLevel = LogLevel.Information)
        => this.logService.GetLogWriter<TLogSource>(logLevel);

    private readonly ILogService logService;
}
