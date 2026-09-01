// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// A pair of log source type and <see cref="LogLevel"/>, used as the key of the log broker cache.
/// </summary>
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
