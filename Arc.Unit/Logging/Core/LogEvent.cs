// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public readonly struct LogEvent : IEquatable<LogEvent>
{
    public LogEvent(ILogService logService, Type logSourceType, LogLevel logLevel, long eventId, string message)
    {
        this.LogService = logService;
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
        this.EventId = eventId;
        this.Message = message;
        this.DateTime = DateTimeOffset.UtcNow.AddTicks(LogUnit.OffsetTicks);
    }

    public readonly ILogService LogService;

    public readonly Type LogSourceType;

    public readonly LogLevel LogLevel;

    public readonly long EventId;

    public readonly string Message;

    public readonly DateTimeOffset DateTime;

    public bool Equals(LogEvent other)
        => this.LogSourceType == other.LogSourceType &&
        this.LogLevel == other.LogLevel &&
        this.EventId == other.EventId &&
        this.Message == other.Message;

    public override int GetHashCode()
        => HashCode.Combine(this.LogSourceType, this.LogLevel, this.EventId, this.Message);
}
