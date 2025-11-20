// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Provides an abstraction for console input and output operations.
/// </summary>
public interface IConsoleService
{
    /// <summary>
    /// Writes the specified message to the console without a newline.
    /// </summary>
    /// <param name="message">The message to write. If null, nothing is written.</param>
    public void Write(string? message = default);

    /// <summary>
    /// Writes the specified message to the console followed by a newline.
    /// </summary>
    /// <param name="message">The message to write. If null, only a newline is written.</param>
    public void WriteLine(string? message = default);

    /// <summary>
    /// Reads a line of text from the console asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the read operation.</param>
    /// <returns>A task that represents the asynchronous read operation, containing the input result.</returns>
    public Task<InputResult> ReadLine(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the next key pressed by the user.
    /// </summary>
    /// <param name="intercept">If true, the pressed key is not displayed in the console.</param>
    /// <returns>An object that describes the key that was pressed.</returns>
    public ConsoleKeyInfo ReadKey(bool intercept);

    /// <summary>
    /// Gets a value indicating whether a key press is available to be read.
    /// </summary>
    public bool KeyAvailable { get; }
}
