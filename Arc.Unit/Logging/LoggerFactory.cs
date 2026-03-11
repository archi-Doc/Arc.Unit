// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class LoggerFactory<TLogSource> : ILogger<TLogSource>
{
    public LoggerFactory(ILogService logService)
    {
        this.logService = logService;
    }

    public LogWriter? GetWriter(LogLevel logLevel = LogLevel.Information)
        => this.logService.GetWriter<TLogSource>(logLevel);

    private readonly ILogService logService;
}
