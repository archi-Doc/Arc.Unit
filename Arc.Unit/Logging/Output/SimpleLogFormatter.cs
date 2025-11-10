// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Text;
using Utf8StringInterpolation;

namespace Arc.Unit;

public partial class SimpleLogFormatter
{
    internal const string DefaultLogText = "Default";
    internal const int DefaultPadding = 10;

    public SimpleLogFormatter(SimpleLogFormatterOptions options)
    {
        this.options = options;
    }

    public string Format(LogEvent param)
    {
        StringBuilder sb = new();
        this.Format(sb, param);
        return sb.ToString();
    }

    public void Format(StringBuilder sb, LogEvent param)
    {// Timestamp [Level Source(EventId)] Message
        var logLevelColors = this.GetLogLevelConsoleColors(param.LogLevel);
        var logLevelString = this.GetLogLevelString(param.LogLevel);

        // Timestamp
        var timestampFormat = this.options.TimestampFormat;
        if (timestampFormat != null)
        {
            if (this.options.TimestampLocal)
            {// Local
                sb.Append(param.DateTime.ToLocalTime().ToString(timestampFormat));
            }
            else
            {// Utc
                sb.Append(param.DateTime.ToString(timestampFormat));
            }

            sb.Append(' ');
        }

        this.WriteColoredMessage(sb, "[", ConsoleHelper.DefaultColor, ConsoleColor.DarkGray); // sb.Append('[');

        // Level
        this.WriteColoredMessage(sb, logLevelString, logLevelColors.Background, logLevelColors.Foreground);

        // Source(EventId)
        // var position = sb.Length;
        string source = param.LogSourceType == typeof(DefaultLog) ? string.Empty : param.LogSourceType.Name; // DefaultLogText
        if (param.EventId == 0 || this.options.EventIdFormat == null)
        {
            if (!string.IsNullOrEmpty(source))
            {
                sb.Append($" {source}");
            }
        }
        else
        {
            sb.Append($" {source}({param.EventId.ToString(this.options.EventIdFormat)})");
        }

        this.WriteColoredMessage(sb, "] ", ConsoleHelper.DefaultColor, ConsoleColor.DarkGray); // sb.Append("] ");

        // Message
        var messageColor = param.LogLevel > LogLevel.Debug ? ConsoleColor.White : ConsoleColor.Gray;
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
        writer.AppendUtf8(this.GetLogLevelUtf8String(param.LogLevel));

        // Source(EventId)
        string source = param.LogSourceType == typeof(DefaultLog) ? string.Empty : param.LogSourceType.Name; // DefaultLogText
        if (param.EventId == 0 || this.options.EventIdFormat == null)
        {
            if (!string.IsNullOrEmpty(source))
            {
                writer.Append(' ');
                writer.AppendLiteral(source);
            }
        }
        else
        {
            writer.Append(' ');
            writer.AppendLiteral(source);
            writer.Append('(');
            writer.AppendFormatted(param.EventId, 0, this.options.EventIdFormat);
            writer.Append(')');
        }

        writer.AppendUtf8("] "u8);

        // Message
        writer.Append(param.Message);

        writer.AppendLine();
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

    private string GetLogLevelString(LogLevel logLevel)
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

    private ReadOnlySpan<byte> GetLogLevelUtf8String(LogLevel logLevel)
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

    private ConsoleColorPair GetLogLevelConsoleColors(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Debug => new ConsoleColorPair(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Information => new ConsoleColorPair(ConsoleColor.White, ConsoleColor.Black),
            LogLevel.Warning => new ConsoleColorPair(ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevel.Error => new ConsoleColorPair(ConsoleColor.White, ConsoleColor.DarkRed),
            LogLevel.Fatal => new ConsoleColorPair(ConsoleColor.White, ConsoleColor.DarkRed),
            _ => new ConsoleColorPair(ConsoleHelper.DefaultColor, ConsoleHelper.DefaultColor),
        };
    }

    private SimpleLogFormatterOptions options;
}
