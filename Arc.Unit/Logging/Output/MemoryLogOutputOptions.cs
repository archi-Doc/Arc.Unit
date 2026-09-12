// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Options of <see cref="MemoryLogger"/>.
/// </summary>
public record class MemoryLoggerOptions
{
    /// <summary>
    /// The default value of <see cref="MaxMemoryUsage"/> (100 MB).
    /// </summary>
    public const long DefaultMaxMemoryUsage = 100_000_000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryLoggerOptions"/> class.
    /// </summary>
    public MemoryLoggerOptions()
    {
        this.FormatterOptions = new SimpleLogFormatterOptions(false) with
        {
            TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff K",
        };
    }

    /// <summary>
    /// Gets <see cref="SimpleLogFormatterOptions"/>.
    /// </summary>
    public SimpleLogFormatterOptions FormatterOptions { get; init; }

    /// <summary>
    /// Gets the retained UTF-8 byte limit (zero or negative means unlimited). Buffer capacity and bookkeeping are excluded.
    /// The default is <see cref="DefaultMaxMemoryUsage"/>. An oversized line evicts prior lines and is discarded.
    /// </summary>
    public long MaxMemoryUsage { get; init; } = DefaultMaxMemoryUsage;
}
