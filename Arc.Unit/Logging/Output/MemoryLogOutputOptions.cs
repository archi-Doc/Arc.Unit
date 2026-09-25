// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Options of <see cref="MemoryLogOutput"/>.
/// </summary>
public record class MemoryLogOutputOptions
{
    /// <summary>
    /// The default value of <see cref="MaxRetainedBytes"/> (100 MB).
    /// </summary>
    public const long DefaultMaxRetainedBytes = 100_000_000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryLogOutputOptions"/> class.
    /// </summary>
    public MemoryLogOutputOptions()
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
    /// Gets the retained UTF-8 byte limit (zero or negative means unlimited). The storage does not grow beyond it; bookkeeping is excluded.
    /// The default is <see cref="DefaultMaxRetainedBytes"/>. An oversized line evicts prior lines and is discarded.
    /// </summary>
    public long MaxRetainedBytes { get; init; } = DefaultMaxRetainedBytes;
}
