// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public static class ConsoleHelper
{
    public const ConsoleColor DefaultColor = (ConsoleColor)(-1);
    public const string DefaultForegroundColor = "\u001b[39m\u001b[22m"; // reset to default foreground color
    public const string DefaultBackgroundColor = "\u001b[49m"; // reset to the background color

    public static ReadOnlySpan<char> EraseToEndOfLineSpan => "\u001b[K";

    public static ReadOnlySpan<char> EraseEntireLineSpan => "\u001b[2K";

    public static ReadOnlySpan<char> ResetSpan => "\u001b[0m";

    public static ReadOnlySpan<char> SaveCursorSpan => "\u001b[s";

    public static ReadOnlySpan<char> RestoreCursorSpan => "\u001b[u";

    public static ReadOnlySpan<char> HideCursorSpan => "\u001b[?25l";

    public static ReadOnlySpan<char> ShowCursorSpan => "\u001b[?25h";

    public static ReadOnlySpan<char> SetCursorSpan => "\u001b["; // "\e[n;mH

    public static string GetForegroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\u001b[30m",
            ConsoleColor.DarkRed => "\u001b[31m",
            ConsoleColor.DarkGray => "\u001b[90m",
            ConsoleColor.DarkGreen => "\u001b[32m",
            ConsoleColor.DarkYellow => "\u001b[33m",
            ConsoleColor.DarkBlue => "\u001b[34m",
            ConsoleColor.DarkMagenta => "\u001b[35m",
            ConsoleColor.DarkCyan => "\u001b[36m",
            ConsoleColor.Gray => "\u001b[37m",
            ConsoleColor.Red => "\u001b[1m\u001b[31m",
            ConsoleColor.Green => "\u001b[1m\u001b[32m",
            ConsoleColor.Yellow => "\u001b[1m\u001b[33m",
            ConsoleColor.Blue => "\u001b[1m\u001b[34m",
            ConsoleColor.Magenta => "\u001b[1m\u001b[35m",
            ConsoleColor.Cyan => "\u001b[1m\u001b[36m",
            ConsoleColor.White => "\u001b[1m\u001b[37m",
            _ => DefaultForegroundColor,
        };
    }

    public static string GetBackgroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\u001b[40m",
            ConsoleColor.DarkRed => "\u001b[41m",
            ConsoleColor.DarkGreen => "\u001b[42m",
            ConsoleColor.DarkYellow => "\u001b[43m",
            ConsoleColor.DarkBlue => "\u001b[44m",
            ConsoleColor.DarkMagenta => "\u001b[45m",
            ConsoleColor.DarkCyan => "\u001b[46m",
            ConsoleColor.Gray => "\u001b[47m",
            _ => DefaultBackgroundColor,
        };
    }

    public static bool TryGetForegroundColor(int number, bool isBright, out ConsoleColor? color)
    {
        color = number switch
        {
            30 => ConsoleColor.Black,
            31 => isBright ? ConsoleColor.Red : ConsoleColor.DarkRed,
            32 => isBright ? ConsoleColor.Green : ConsoleColor.DarkGreen,
            33 => isBright ? ConsoleColor.Yellow : ConsoleColor.DarkYellow,
            34 => isBright ? ConsoleColor.Blue : ConsoleColor.DarkBlue,
            35 => isBright ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta,
            36 => isBright ? ConsoleColor.Cyan : ConsoleColor.DarkCyan,
            37 => isBright ? ConsoleColor.White : ConsoleColor.Gray,
            _ => null,
        };

        return color != null || number == 39;
    }

    public static bool TryGetBackgroundColor(int number, out ConsoleColor? color)
    {
        color = number switch
        {
            40 => ConsoleColor.Black,
            41 => ConsoleColor.DarkRed,
            42 => ConsoleColor.DarkGreen,
            43 => ConsoleColor.DarkYellow,
            44 => ConsoleColor.DarkBlue,
            45 => ConsoleColor.DarkMagenta,
            46 => ConsoleColor.DarkCyan,
            47 => ConsoleColor.Gray,
            _ => null,
        };

        return color != null || number == 49;
    }
}
