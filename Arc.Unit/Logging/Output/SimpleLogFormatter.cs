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

    public SimpleLogFormatter(SimpleLogFormatterOptions options)
    {
        this.options = options;
    }

    public string Format(LogEvent param)
    {
        var sb = RentStringBuilder();
        this.Format(sb, param);
        var result = sb.ToString();
        ReturnStringBuilder(sb);
        return result;
    }

    public void Format(StringBuilder sb, LogEvent param)
    {// Timestamp [Level Source(EventId)] Message
        var logLevelColors = GetLogLevelConsoleColors(param.LogLevel);
        var logLevelString = GetLogLevelString(param.LogLevel);

        // Colors
        var sourceColor = this.options.SourceColor;
        var messageColor = this.options.MessageColor;
        if (param.LogLevel <= LogLevel.Debug)
        {
            sourceColor = ConsoleColor.Gray;
            messageColor = ConsoleColor.Gray;
        }
        else if (param.LogLevel >= LogLevel.Error)
        {
            messageColor = ConsoleColor.Red;
        }

        // Timestamp
        var timestampFormat = this.options.TimestampFormat;
        if (timestampFormat != null)
        {
            var dateTime = this.options.TimestampLocal ? param.DateTime.ToLocalTime() : param.DateTime;
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
        if (param.LogSourceType != typeof(DefaultLog))
        {
            sb.Append(' ');
            this.WriteColoredMessage(sb, param.LogSourceType.Name, ConsoleHelper.DefaultColor, sourceColor);
        }

        if (param.EventId != 0 && this.options.EventIdFormat is { } eventIdFormat)
        {
            Span<char> destination = stackalloc char[FormatBufferLength];
            sb.Append('(');
            if (param.EventId.TryFormat(destination, out var written, eventIdFormat))
            {
                sb.Append(destination.Slice(0, written));
            }
            else
            {
                sb.Append(param.EventId.ToString(eventIdFormat));
            }

            sb.Append(')');
        }

        sb.Append("] ");

        // Message
        this.WriteColoredMessage(sb, param.Message, ConsoleHelper.DefaultColor, messageColor);
    }

    public byte[] FormatUtf8(LogEvent param)
    {
        using var buffer = Utf8String.CreateWriter(out var writer);
        this.FormatUtf8(ref writer, param);
        writer.Flush();
        return buffer.ToArray();
    }

    public void FormatUtf8(ref Utf8StringWriter<ArrayBufferWriter<byte>> writer, LogEvent param)
    {// Timestamp [Level Source(EventId)] Message
        // Timestamp
        var timestampFormat = this.options.TimestampFormat;
        if (timestampFormat != null)
        {
            if (this.options.TimestampLocal)
            {// Local
                writer.AppendFormatted(param.DateTime.ToLocalTime(), 0, timestampFormat);
            }
            else
            {// Utc
                writer.AppendFormatted(param.DateTime, 0, timestampFormat);
            }

            writer.Append(' ');
        }

        writer.Append('[');
        writer.AppendUtf8(GetLogLevelUtf8String(param.LogLevel));

        // Source(EventId)
        if (param.LogSourceType != typeof(DefaultLog))
        {
            writer.Append(' ');
            writer.AppendLiteral(param.LogSourceType.Name);
        }

        if (param.EventId != 0 && this.options.EventIdFormat is { } eventIdFormat)
        {
            writer.Append('(');
            writer.AppendFormatted(param.EventId, 0, eventIdFormat);
            writer.Append(')');
        }

        writer.AppendUtf8("] "u8);

        // Message
        writer.Append(param.Message);

        writer.AppendLine();
    }

    /// <summary>
    /// Formats the log event and writes it to the console service (no string is allocated).
    /// </summary>
    /// <param name="consoleService"><see cref="IConsoleService"/>.</param>
    /// <param name="param"><see cref="LogEvent"/>.</param>
    internal void FormatAndWriteLine(IConsoleService consoleService, LogEvent param)
    {
        var sb = RentStringBuilder();
        this.Format(sb, param);

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
            sb.Append(ConsoleHelper.DefaultForegroundColor); // reset to default foreground color
        }

        if (background != ConsoleHelper.DefaultColor)
        {
            sb.Append(ConsoleHelper.DefaultBackgroundColor); // reset to the background color
        }
    }
}
