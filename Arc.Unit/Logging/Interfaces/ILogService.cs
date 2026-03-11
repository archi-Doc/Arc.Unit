// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Defines a centralized logging service that provides typed logger access, <br/>
/// level-filtered writer creation, and console output integration.
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Gets a <see cref="LogWriter"/> for the specified source type and log level.
    /// </summary>
    /// <typeparam name="TLogSource">
    /// The source type used to categorize and route log messages.
    /// </typeparam>
    /// <param name="logLevel">
    /// The log level for the requested writer. Defaults to <see cref="LogLevel.Information"/>.
    /// </param>
    /// <returns>
    /// A configured <see cref="LogWriter"/> when logging is available for the requested level; otherwise, <see langword="null"/>.
    /// </returns>
    LogWriter? GetWriter<TLogSource>(LogLevel logLevel = LogLevel.Information);

    /// <summary>
    /// Gets a typed logger associated with the specified source type.
    /// </summary>
    /// <typeparam name="TLogSource">
    /// The source type used to categorize and route log messages.
    /// </typeparam>
    /// <returns>
    /// An <see cref="ILogger{TLogSource}"/> instance for the specified source type.
    /// </returns>
    ILogger<TLogSource> GetLogger<TLogSource>();

    /// <summary>
    /// Gets a logger associated with the specified source type.
    /// </summary>
    /// <param name="logSource">
    /// The source type used to categorize and route log messages.
    /// </param>
    /// <returns>
    /// An <see cref="ILogger"/> instance for the specified source type.
    /// </returns>
    ILogger GetLogger(Type logSource);

    /// <summary>
    /// Gets the console service used for console-oriented output operations.
    /// </summary>
    IConsoleService ConsoleService { get; }
}
