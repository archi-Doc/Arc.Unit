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
    /// Gets the maximum memory usage in bytes (0 for unlimited, default value is <see cref="DefaultMaxMemoryUsage"/>).
    /// </summary>
    public long MaxMemoryUsage { get; init; } = DefaultMaxMemoryUsage;
}
