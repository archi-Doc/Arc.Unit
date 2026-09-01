// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal readonly struct LogSourceLevelPair : IEquatable<LogSourceLevelPair>
{
    public LogSourceLevelPair(Type logSourceType, LogLevel logLevel)
    {
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
    }

    public readonly Type LogSourceType;

    public readonly LogLevel LogLevel;

    public override bool Equals(object? obj)
        => obj is LogSourceLevelPair other && this.Equals(other);

    public bool Equals(LogSourceLevelPair other)
        => this.LogSourceType == other.LogSourceType &&
            this.LogLevel == other.LogLevel;

    public override int GetHashCode()
        => HashCode.Combine(this.LogSourceType, this.LogLevel);
}
