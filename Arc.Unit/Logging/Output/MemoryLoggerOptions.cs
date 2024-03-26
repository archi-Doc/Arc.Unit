// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public record class MemoryLoggerOptions
{
    public const long DefaultMaxMemoryUsage = 100_000_000; // 100 MB

    public MemoryLoggerOptions()
    {
        this.Formatter = new(false);
        this.Formatter.TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff K";
    }

    /// <summary>
    /// Gets <see cref="SimpleLogFormatterOptions"/>.
    /// </summary>
    public SimpleLogFormatterOptions Formatter { get; init; }

    /// <summary>
    /// Gets or sets the maximum memory usage in bytes (0 for unlimited, default value is <see cref="DefaultMaxMemoryUsage"/>).
    /// </summary>
    public long MaxMemoryUsage { get; set; } = DefaultMaxMemoryUsage;
}
