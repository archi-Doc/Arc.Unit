// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;

namespace Arc.Unit;

/// <summary>
/// The default <see cref="IConsoleService"/> which reads from and writes to <see cref="Console"/>.<br/>
/// Exceptions are ignored, since console operations may fail after the console window is closed.
/// </summary>
public class ConsoleService : IConsoleService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleService"/> class.
    /// </summary>
    public ConsoleService()
    {
    }

    /// <inheritdoc/>
    public void Write(string? message = default, ConsoleColor color = ConsoleHelper.DefaultColor)
        => this.Write(message.AsSpan(), color);

    /// <inheritdoc/>
    public void Write(ReadOnlySpan<char> message, ConsoleColor color = ConsoleHelper.DefaultColor)
    {
        if (message.IsEmpty)
        {
            return;
        }
        else if (!this.EnableColor || color == ConsoleHelper.DefaultColor)
        {
            TryWrite(message, false);
            return;
        }

        WriteColored(message, color, false);
    }

    /// <inheritdoc/>
    public void WriteLine(string? message = default, ConsoleColor color = ConsoleHelper.DefaultColor)
        => this.WriteLine(message.AsSpan(), color);

    /// <inheritdoc/>
    public void WriteLine(ReadOnlySpan<char> message, ConsoleColor color = ConsoleHelper.DefaultColor)
    {
        if (message.IsEmpty || !this.EnableColor || color == ConsoleHelper.DefaultColor)
        {
            TryWrite(message, true);
            return;
        }

        WriteColored(message, color, true);
    }

    /// <inheritdoc/>
    public async Task<InputResult> ReadLine(CancellationToken cancellationToken)
    {
        try
        {
            try
            {
                var text = await Console.In.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                return new(text ?? string.Empty);
            }
            catch (OperationCanceledException)
            {
                return new(InputResultKind.Canceled);
            }
        }
        catch
        {
            return new(InputResultKind.Terminated);
        }
    }

    /// <inheritdoc/>
    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        try
        {
            return Console.ReadKey(intercept);
        }
        catch
        {
            return default;
        }
    }

    /// <inheritdoc/>
    public bool KeyAvailable
    {
        get
        {
            try
            {
                return Console.KeyAvailable;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc/>
    public bool EnableColor { get; set; } = true;

    /// <summary>
    /// Writes the message enclosed in the foreground color escape sequence and the reset sequence.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="color">The foreground color.</param>
    /// <param name="newLine"><see langword="true"/> to append a line terminator.</param>
    private static void WriteColored(ReadOnlySpan<char> message, ConsoleColor color, bool newLine)
    {
        var prefix = ConsoleHelper.GetForegroundColorEscapeCode(color).AsSpan();
        var suffix = ConsoleHelper.ResetSpan;
        var length = prefix.Length + message.Length + suffix.Length;

        char[]? rent = null;
        Span<char> buffer = length <= BaseHelper.StackallocThreshold ?
            stackalloc char[length] : (rent = ArrayPool<char>.Shared.Rent(length));
        buffer = buffer.Slice(0, length); // ArrayPool may return a larger array.

        prefix.CopyTo(buffer);
        message.CopyTo(buffer.Slice(prefix.Length));
        suffix.CopyTo(buffer.Slice(prefix.Length + message.Length));

        try
        {
            TryWrite(buffer, newLine);
        }
        finally
        {
            if (rent is not null)
            {
                ArrayPool<char>.Shared.Return(rent);
            }
        }
    }

    /// <summary>
    /// Writes to the console, ignoring exceptions (Console output might throw after the console window is closed).
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="newLine"><see langword="true"/> to append a line terminator.</param>
    private static void TryWrite(ReadOnlySpan<char> message, bool newLine)
    {
        try
        {
            if (newLine)
            {
                Console.Out.WriteLine(message);
            }
            else
            {
                Console.Out.Write(message);
            }
        }
        catch
        {
        }
    }
}
