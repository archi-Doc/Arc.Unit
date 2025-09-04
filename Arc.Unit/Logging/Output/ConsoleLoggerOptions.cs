// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public record class ConsoleLoggerOptions
{
    public const int DefaultMaxQueue = 1_000;

    public ConsoleLoggerOptions()
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
    /// Gets the maximum number of queued log (0 for unlimited).
    /// </summary>
    public int MaxQueue { get; init; } = DefaultMaxQueue;
}
