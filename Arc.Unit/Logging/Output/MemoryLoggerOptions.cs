// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public record class MemoryLoggerOptions
{
    public const int DefaultMaxMemoryUsage = 100;

    public MemoryLoggerOptions()
    {
        this.Formatter = new(true);
    }

    /// <summary>
    /// Gets <see cref="SimpleLogFormatterOptions"/>.
    /// </summary>
    public SimpleLogFormatterOptions Formatter { get; init; }

    /// <summary>
    /// Gets or sets the maximum memory usage in megabytes (0 for unlimited, default value is <see cref="DefaultMaxMemoryUsage"/>).
    /// </summary>
    public int MaxMemoryUsage { get; set; } = DefaultMaxMemoryUsage;
}
