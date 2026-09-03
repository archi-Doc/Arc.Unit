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

/// <summary>
/// The non-generic <see cref="ILogger"/> implementation bound to a log source <see cref="Type"/> which is known only at runtime.
/// </summary>
/// <remarks>
/// This is used by <see cref="ILogService.GetLogger(Type)"/>, so that the logger can be created
/// without constructing a generic type at runtime (which is not supported by Native AOT).
/// </remarks>
internal sealed class LoggerFactory : ILogger
{
    public LoggerFactory(ILogService logService, LogUnit logUnit, Type logSourceType)
    {
        this.logService = logService;
        this.logUnit = logUnit;
        this.logSourceType = logSourceType;
    }

    public LogWriter? GetWriter(LogLevel logLevel = LogLevel.Information)
    {
        var broker = this.logUnit.GetLogBroker(this.logSourceType, logLevel);
        if (broker is null)
        {
            return default;
        }

        return new(this.logService, broker);
    }

    private readonly ILogService logService;
    private readonly LogUnit logUnit;
    private readonly Type logSourceType;
}
