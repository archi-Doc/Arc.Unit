// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// The default <see cref="ILogger{TLogSource}"/> implementation which delegates to <see cref="ILogService"/>.
/// </summary>
/// <typeparam name="TLogSource">The log source type.</typeparam>
internal sealed class LoggerFactory<TLogSource> : ILogger<TLogSource>
{
    public LoggerFactory(ILogService logService)
    {
        this.logService = logService;
    }

    public LogWriter? GetWriter(LogLevel logLevel = LogLevel.Information)
        => this.logService.GetWriter<TLogSource>(logLevel);

    private readonly ILogService logService;
}
