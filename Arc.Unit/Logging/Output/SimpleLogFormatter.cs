// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Text;
using Utf8StringInterpolation;

namespace Arc.Unit;

/// <summary>
/// Formats a <see cref="LogEvent"/> into the form of "Timestamp [Level Source(EventId)] Message".
/// </summary>
public class SimpleLogFormatter
{
    private const int FormatBufferLength = 64;
    private const int InitialStringBuilderCapacity = 256;
    private const int MaxCachedStringBuilderCapacity = 8 * 1024;

    [ThreadStatic]
    private static StringBuilder? cachedStringBuilder;

    private readonly SimpleLogFormatterOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleLogFormatter"/> class.
    /// </summary>
    /// <param name="options"><see cref="SimpleLogFormatterOptions"/>.</param>
    public SimpleLogFormatter(SimpleLogFormatterOptions options)
    {
        this.options = options;
    }

    /// <summary>
    /// Formats the log event into a string.
    /// </summary>
    /// <param name="logEvent">The log event to be formatted.</param>
    /// <returns>The formatted text.</returns>
    public string Format(LogEvent logEvent)
    {
        var sb = RentStringBuilder();
        this.Format(sb, logEvent);
        var result = sb.ToString();
        ReturnStringBuilder(sb);
        return result;
    }

    /// <summary>
    /// Formats the log event and appends it to the specified <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> to append to.</param>
    /// <param name="logEvent">The log event to be formatted.</param>
    public void Format(StringBuilder sb, LogEvent logEvent)
    {// Timestamp [Level Source(EventId)] Message
        var logLevelColors = GetLogLevelConsoleColors(logEvent.LogLevel);
        var logLevelString = GetLogLevelString(logEvent.LogLevel);

        // Colors
        var sourceColor = this.options.SourceColor;
        var messageColor = this.options.MessageColor;
        if (logEvent.LogLevel <= LogLevel.Debug)
        {
            sourceColor = ConsoleColor.Gray;
            messageColor = ConsoleColor.Gray;
        }
        else if (logEvent.LogLevel >= LogLevel.Error)
        {
            messageColor = ConsoleColor.Red;
        }

        // Timestamp
        var timestampFormat = this.options.TimestampFormat;
        if (timestampFormat != null)
        {
            var dateTime = this.options.TimestampLocal ? logEvent.Timestamp.ToLocalTime() : logEvent.Timestamp;
            Span<char> destination = stackalloc char[FormatBufferLength];
            if (dateTime.TryFormat(destination, out var written, timestampFormat))
            {
                sb.Append(destination.Slice(0, written));
            }
            else
            {
                sb.Append(dateTime.ToString(timestampFormat));
            }

            sb.Append(' ');
        }

        sb.Append('[');

        // Level
        this.WriteColoredMessage(sb, logLevelString, logLevelColors.Background, logLevelColors.Foreground);

        // Source(EventId)
        if (logEvent.LogSourceType != typeof(DefaultLog))
        {
            sb.Append(' ');
            this.WriteColoredMessage(sb, logEvent.LogSourceType.Name, ConsoleHelper.DefaultColor, sourceColor);
        }

        if (logEvent.EventId != 0 && this.options.EventIdFormat is { } eventIdFormat)
        {
            Span<char> destination = stackalloc char[FormatBufferLength];
            sb.Append('(');
            if (logEvent.EventId.TryFormat(destination, out var written, eventIdFormat))
            {
                sb.Append(destination.Slice(0, written));
            }
            else
            {
                sb.Append(logEvent.EventId.ToString(eventIdFormat));
            }

            sb.Append(')');
        }

        sb.Append("] ");

        // Message
        this.WriteColoredMessage(sb, logEvent.Message, ConsoleHelper.DefaultColor, messageColor);
    }

    /// <summary>
    /// Formats the log event into a UTF-8 byte array (a line terminator is appended).
    /// </summary>
    /// <param name="logEvent">The log event to be formatted.</param>
    /// <returns>The formatted UTF-8 text.</returns>
    public byte[] FormatUtf8(LogEvent logEvent)
    {
        using var buffer = Utf8String.CreateWriter(out var writer);
        this.FormatUtf8(ref writer, logEvent);
        writer.Flush();
        return buffer.ToArray();
    }

    /// <summary>
    /// Formats the log event and writes it to the specified writer (a line terminator is appended).
    /// </summary>
    /// <param name="writer">The UTF-8 writer to write to.</param>
    /// <param name="logEvent">The log event to be formatted.</param>
    public void FormatUtf8(ref Utf8StringWriter<ArrayBufferWriter<byte>> writer, LogEvent logEvent)
    {// Timestamp [Level Source(EventId)] Message
        // Timestamp
        var timestampFormat = this.options.TimestampFormat;
        if (timestampFormat != null)
        {
            if (this.options.TimestampLocal)
            {// Local
                writer.AppendFormatted(logEvent.Timestamp.ToLocalTime(), 0, timestampFormat);
            }
            else
            {// Utc
                writer.AppendFormatted(logEvent.Timestamp, 0, timestampFormat);
            }

            writer.Append(' ');
        }

        writer.Append('[');
        writer.AppendUtf8(GetLogLevelUtf8String(logEvent.LogLevel));

        // Source(EventId)
        if (logEvent.LogSourceType != typeof(DefaultLog))
        {
            writer.Append(' ');
            writer.AppendLiteral(logEvent.LogSourceType.Name);
        }

        if (logEvent.EventId != 0 && this.options.EventIdFormat is { } eventIdFormat)
        {
            writer.Append('(');
            writer.AppendFormatted(logEvent.EventId, 0, eventIdFormat);
            writer.Append(')');
        }

        writer.AppendUtf8("] "u8);

        // Message
        writer.Append(logEvent.Message);

        writer.AppendLine();
    }

    /// <summary>
    /// Formats the log event and writes it to the console service (no string is allocated).
    /// </summary>
    /// <param name="consoleService"><see cref="IConsoleService"/>.</param>
    /// <param name="logEvent"><see cref="LogEvent"/>.</param>
    internal void FormatAndWriteLine(IConsoleService consoleService, LogEvent logEvent)
    {
        var sb = RentStringBuilder();
        this.Format(sb, logEvent);

        var length = sb.Length;
        char[]? rent = null;
        Span<char> buffer = length <= BaseHelper.StackallocThreshold ?
            stackalloc char[length] : (rent = ArrayPool<char>.Shared.Rent(length));
        buffer = buffer.Slice(0, length); // ArrayPool may return a larger array.

        sb.CopyTo(0, buffer, length);
        ReturnStringBuilder(sb);

        try
        {
            consoleService.WriteLine(buffer);
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
    /// Rents the cached <see cref="StringBuilder"/> (the field is cleared, so that a reentrant call creates another instance).
    /// </summary>
    /// <returns>The <see cref="StringBuilder"/> to be used.</returns>
    private static StringBuilder RentStringBuilder()
    {
        var sb = cachedStringBuilder;
        if (sb is null)
        {
            return new StringBuilder(InitialStringBuilderCapacity);
        }

        cachedStringBuilder = null;
        sb.Clear();
        return sb;
    }

    private static void ReturnStringBuilder(StringBuilder sb)
    {
        if (sb.Capacity <= MaxCachedStringBuilderCapacity)
        {// A large buffer is not cached.
            cachedStringBuilder = sb;
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Fatal => "FTL",
            _ => string.Empty,
        };
    }

    private static ReadOnlySpan<byte> GetLogLevelUtf8String(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Debug => "DBG"u8,
            LogLevel.Information => "INF"u8,
            LogLevel.Warning => "WRN"u8,
            LogLevel.Error => "ERR"u8,
            LogLevel.Fatal => "FTL"u8,
            _ => ""u8,
        };
    }

    private static ConsoleColorPair GetLogLevelConsoleColors(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Debug => new ConsoleColorPair(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Information => new ConsoleColorPair(ConsoleColor.White, ConsoleColor.Black),
            LogLevel.Warning => new ConsoleColorPair(ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevel.Error => new ConsoleColorPair(ConsoleColor.Red, ConsoleColor.Black),
            LogLevel.Fatal => new ConsoleColorPair(ConsoleColor.White, ConsoleColor.DarkRed),
            _ => new ConsoleColorPair(ConsoleHelper.DefaultColor, ConsoleHelper.DefaultColor),
        };
    }

    private void WriteColoredMessage(StringBuilder sb, string message, ConsoleColor background, ConsoleColor foreground)
    {
        if (!this.options.EnableColor)
        {
            sb.Append(message);
            return;
        }

        if (background != ConsoleHelper.DefaultColor)
        {
            sb.Append(ConsoleHelper.GetBackgroundColorEscapeCode(background));
        }

        if (foreground != ConsoleHelper.DefaultColor)
        {
            sb.Append(ConsoleHelper.GetForegroundColorEscapeCode(foreground));
        }

        sb.Append(message);

        if (foreground != ConsoleHelper.DefaultColor)
        {
            sb.Append(ConsoleHelper.DefaultForegroundColorEscapeCode); // reset to default foreground color
        }

        if (background != ConsoleHelper.DefaultColor)
        {
            sb.Append(ConsoleHelper.DefaultBackgroundColorEscapeCode); // reset to the background color
        }
    }
}
