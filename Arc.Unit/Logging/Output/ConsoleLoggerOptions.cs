// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Options of <see cref="ConsoleLogger"/>.
/// </summary>
public record class ConsoleLoggerOptions
{
    /// <summary>
    /// The default value of <see cref="MaxQueue"/>.
    /// </summary>
    public const int DefaultMaxQueue = 1_000;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleLoggerOptions"/> class.
    /// </summary>
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
    /// Gets the maximum number of queued log (0 for unlimited).<br/>
    /// This is used only when <see cref="EnableBuffering"/> is <see langword="true"/>.
    /// </summary>
    public int MaxQueue { get; init; } = DefaultMaxQueue;
}
