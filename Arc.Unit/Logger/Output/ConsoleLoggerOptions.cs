// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public record class ConsoleLoggerOptions
{
    public const int DefaultMaxQueue = 1_000;

    public ConsoleLoggerOptions()
    {
        this.Formatter = new(true);
    }

    /// <summary>
    /// Gets or sets a value indicating whether logs are buffered for a set period (default is 40 milliseconds) and then output together.<br/>
    /// This improves performance during log output but may result in logs being out of order with other console outputs.
    /// </summary>
    public bool EnableBuffering { get; set; } = false;

    /// <summary>
    /// Gets <see cref="SimpleLogFormatterOptions"/>.
    /// </summary>
    public SimpleLogFormatterOptions Formatter { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of queued log (0 for unlimited).
    /// </summary>
    public int MaxQueue { get; set; } = DefaultMaxQueue;
}
