// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Provides console escape sequences (ANSI/VT100) and color conversion helpers.
/// </summary>
public static class ConsoleHelper
{
    /// <summary>
    /// Represents the default console color (the color is not changed).
    /// </summary>
    public const ConsoleColor DefaultColor = (ConsoleColor)(-1);

    /// <summary>
    /// The escape sequence which resets the foreground color to the default.
    /// </summary>
    public const string DefaultForegroundColorEscapeCode = "\u001b[39m\u001b[22m"; // reset to default foreground color

    /// <summary>
    /// The escape sequence which resets the background color to the default.
    /// </summary>
    public const string DefaultBackgroundColorEscapeCode = "\u001b[49m"; // reset to the background color

    /// <summary>
    /// Provides extension properties for <see cref="InputResultKind"/> to simplify result checks.
    /// </summary>
    /// <param name="inputResultKind">The <see cref="InputResultKind"/> value to evaluate.</param>
    extension(InputResultKind inputResultKind)
    {
        /// <summary>
        /// Gets a value indicating whether the result is positive (success or yes).
        /// </summary>
        public bool IsPositive => inputResultKind == InputResultKind.Success;

        /// <summary>
        /// Gets a value indicating whether the result is negative (no).
        /// </summary>
        public bool IsNegative => inputResultKind == InputResultKind.No;

        /// <summary>
        /// Gets a value indicating whether the input is canceled.
        /// </summary>
        public bool IsCanceled => inputResultKind == InputResultKind.Canceled;

        /// <summary>
        /// Gets a value indicating whether the input is terminated.
        /// </summary>
        public bool IsTerminated => inputResultKind == InputResultKind.Terminated;
    }

    /// <summary>
    /// Gets the line terminator of the current environment.
    /// </summary>
    public static ReadOnlySpan<char> NewLineSpan => Environment.NewLine;

    /// <summary>
    /// Gets the escape sequence which erases from the cursor to the end of the line.
    /// </summary>
    public static ReadOnlySpan<char> EraseToEndOfLineSpan => "\u001b[K";

    /// <summary>
    /// Gets the escape sequence which erases to the end of the line, followed by a line terminator.
    /// </summary>
    public static ReadOnlySpan<char> EraseToEndOfLineAndNewLineSpan => Environment.NewLine == "\r\n" ? "\u001b[K\r\n" : "\u001b[K\n";

    /// <summary>
    /// Gets the escape sequence which erases the entire line.
    /// </summary>
    public static ReadOnlySpan<char> EraseEntireLineSpan => "\u001b[2K";

    /// <summary>
    /// Gets the escape sequence which erases the entire line, followed by a line terminator.
    /// </summary>
    public static ReadOnlySpan<char> EraseEntireLineAndNewLineSpan => Environment.NewLine == "\r\n" ? "\u001b[2K\r\n" : "\u001b[2K\n";

    /// <summary>
    /// Gets the escape sequence which resets all the display attributes.
    /// </summary>
    public static ReadOnlySpan<char> ResetSpan => "\u001b[0m";

    /// <summary>
    /// Gets the escape sequence which saves the cursor position.
    /// </summary>
    public static ReadOnlySpan<char> SaveCursorSpan => "\u001b[s";

    /// <summary>
    /// Gets the escape sequence which restores the saved cursor position.
    /// </summary>
    public static ReadOnlySpan<char> RestoreCursorSpan => "\u001b[u";

    /// <summary>
    /// Gets the escape sequence which hides the cursor.
    /// </summary>
    public static ReadOnlySpan<char> HideCursorSpan => "\u001b[?25l";

    /// <summary>
    /// Gets the escape sequence which shows the cursor.
    /// </summary>
    public static ReadOnlySpan<char> ShowCursorSpan => "\u001b[?25h";

    /// <summary>
    /// Gets the prefix of the cursor position sequence (row and column follow).
    /// </summary>
    public static ReadOnlySpan<char> SetCursorSpan => "\u001b["; // "\e[n;mH

    /// <summary>
    /// Gets the escape sequence which moves the cursor to the upper left corner.
    /// </summary>
    public static ReadOnlySpan<char> ResetCursorSpan => "\u001b[0;0H";

    /// <summary>
    /// Gets the escape sequence which sets the specified foreground color.
    /// </summary>
    /// <param name="color">The foreground color.</param>
    /// <returns>The escape sequence (<see cref="DefaultForegroundColorEscapeCode"/> if the color is not supported).</returns>
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
            _ => DefaultForegroundColorEscapeCode,
        };
    }

    /// <summary>
    /// Gets the escape sequence which sets the specified background color.
    /// </summary>
    /// <param name="color">The background color.</param>
    /// <returns>The escape sequence (<see cref="DefaultBackgroundColorEscapeCode"/> if the color is not supported).</returns>
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
            _ => DefaultBackgroundColorEscapeCode,
        };
    }

    /// <summary>
    /// Converts an SGR parameter (30-37, 39) into a foreground <see cref="ConsoleColor"/>.
    /// </summary>
    /// <param name="code">The SGR parameter of the escape sequence.</param>
    /// <param name="isBright"><see langword="true"/> if the bright (bold) attribute is set.</param>
    /// <param name="color">When this method returns, contains the color, or <see langword="null"/> for the default color.</param>
    /// <returns><see langword="true"/> if <paramref name="code"/> is a foreground color parameter.</returns>
    public static bool TryGetForegroundColor(int code, bool isBright, out ConsoleColor? color)
    {
        color = code switch
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

        return color != null || code == 39;
    }

    /// <summary>
    /// Converts an SGR parameter (40-47, 49) into a background <see cref="ConsoleColor"/>.
    /// </summary>
    /// <param name="code">The SGR parameter of the escape sequence.</param>
    /// <param name="color">When this method returns, contains the color, or <see langword="null"/> for the default color.</param>
    /// <returns><see langword="true"/> if <paramref name="code"/> is a background color parameter.</returns>
    public static bool TryGetBackgroundColor(int code, out ConsoleColor? color)
    {
        color = code switch
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

        return color != null || code == 49;
    }
}
