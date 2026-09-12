// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Options of <see cref="ConsoleLogOutput"/>.
/// </summary>
public record class ConsoleLogOutputOptions
{
    /// <summary>
    /// The default value of <see cref="MaxQueueLength"/>.
    /// </summary>
    public const int DefaultMaxQueueLength = 1_000;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLogOutputOptions"/> class.
    /// </summary>
    public ConsoleLogOutputOptions()
    {
        this.FormatterOptions = new(true);
    }

    /// <summary>
    /// Gets a value indicating whether logs are buffered for a set period (default is 40 milliseconds) and then output together.<br/>
    /// This improves performance during log output but may result in logs being out of order with other console outputs.
    /// </summary>
    public bool EnableBuffering { get; init; } = false;

    /// <summary>
    /// Gets <see cref="SimpleLogFormatterOptions"/>.
    /// </summary>
    public SimpleLogFormatterOptions FormatterOptions { get; init; }

    /// <summary>
    /// Gets the maximum queued event count (zero or negative means unlimited). New events are dropped when full.<br/>
    /// This is used only when <see cref="EnableBuffering"/> is <see langword="true"/>.
    /// </summary>
    public int MaxQueueLength { get; init; } = DefaultMaxQueueLength;
}
