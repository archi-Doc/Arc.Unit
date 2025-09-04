// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public record class SimpleLogFormatterOptions
{
    public SimpleLogFormatterOptions(bool enableColor)
    {
        this.EnableColor = enableColor;
    }

    public bool EnableColor { get; init; }

    /// <summary>
    /// Gets the timestamp format (default is "HH:mm:ss.fff").
    /// </summary>
    public string? TimestampFormat { get; init; } = "HH:mm:ss.fff";

    /// <summary>
    /// Gets a value indicating whether timestamps are displayed as local time or not.
    /// </summary>
    public bool TimestampLocal { get; init; } = true;

    /// <summary>
    /// Gets the event id format (default is "X4").
    /// </summary>
    public string? EventIdFormat { get; init; } = "X4";
}
