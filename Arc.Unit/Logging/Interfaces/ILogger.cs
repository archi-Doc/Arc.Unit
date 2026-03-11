// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogger
{
    /// <summary>
    /// Gets a <see cref="LogWriter"/> configured for the specified log level.
    /// </summary>
    /// <param name="logLevel">
    /// The log level for the requested writer. Defaults to <see cref="LogLevel.Information"/>.
    /// </param>
    /// <returns>
    /// A configured <see cref="LogWriter"/> when logging is available for the log level; otherwise, <see langword="null"/>.
    /// </returns>
    public LogWriter? GetWriter(LogLevel logLevel = LogLevel.Information);
}

/// <summary>
/// Represents a typed logger bound to a specific log source type.
/// </summary>
/// <typeparam name="TLogSource">
/// The source type used to categorize and route log messages.
/// </typeparam>
public interface ILogger<TLogSource> : ILogger
{
}
