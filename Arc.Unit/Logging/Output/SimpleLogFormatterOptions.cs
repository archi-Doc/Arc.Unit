// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Options of <see cref="SimpleLogFormatter"/>.
/// </summary>
public record class SimpleLogFormatterOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleLogFormatterOptions"/> class.
    /// </summary>
    /// <param name="enableColor"><see langword="true"/> to add color escape sequences to the formatted text.</param>
    public SimpleLogFormatterOptions(bool enableColor)
    {
        this.EnableColor = enableColor;
    }

    /// <summary>
    /// Gets a value indicating whether color escape sequences are added to the formatted text.
    /// </summary>
    public bool EnableColor { get; init; }

    /// <summary>
    /// Gets the timestamp format (default is "HH:mm:ss.fff", <see langword="null"/> to omit the timestamp).
    /// </summary>
    public string? TimestampFormat { get; init; } = "HH:mm:ss.fff";

    /// <summary>
    /// Gets a value indicating whether timestamps are displayed as local time or not.
    /// </summary>
    public bool TimestampLocal { get; init; } = true;

    /// <summary>
    /// Gets the event id format (default is "X4", <see langword="null"/> to omit the event id).
    /// </summary>
    public string? EventIdFormat { get; init; } = "X4";

    /// <summary>
    /// Gets the color of the log source name.
    /// </summary>
    public ConsoleColor SourceColor { get; init; } = ConsoleColor.DarkGreen;

    /// <summary>
    /// Gets the color of the message (<see cref="LogLevel.Debug"/> and <see cref="LogLevel.Error"/> or higher use a fixed color).
    /// </summary>
    public ConsoleColor MessageColor { get; init; } = ConsoleColor.White;
}
