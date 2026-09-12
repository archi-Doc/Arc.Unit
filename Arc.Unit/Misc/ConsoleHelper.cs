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
    public const string DefaultForegroundColorEscapeCode = "[39m[22m"; // reset to default foreground color

    /// <summary>
    /// The escape sequence which resets the background color to the default.
    /// </summary>
    public const string DefaultBackgroundColorEscapeCode = "[49m"; // reset to the background color

    /// <summary>
    /// Provides extension properties for <see cref="InputResultKind"/> to simplify result checks.
    /// </summary>
    /// <param name="inputResultKind">The <see cref="InputResultKind"/> value to evaluate.</param>
    extension(InputResultKind inputResultKind)
    {
        /// <summary>
        /// Gets a value indicating whether the result is <see cref="InputResultKind.Success"/> (success or 'Yes').
        /// </summary>
        public bool IsSuccess => inputResultKind == InputResultKind.Success;

        /// <summary>
        /// Gets a value indicating whether the result is <see cref="InputResultKind.No"/>.
        /// </summary>
        public bool IsNo => inputResultKind == InputResultKind.No;

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
    public static ReadOnlySpan<char> EraseToEndOfLineSpan => "[K";

    /// <summary>
    /// Gets the escape sequence which erases to the end of the line, followed by a line terminator.
    /// </summary>
    public static ReadOnlySpan<char> EraseToEndOfLineAndNewLineSpan => Environment.NewLine == "\r\n" ? "[K\r\n" : "[K\n";

    /// <summary>
    /// Gets the escape sequence which erases the entire line.
    /// </summary>
    public static ReadOnlySpan<char> EraseEntireLineSpan => "[2K";

    /// <summary>
    /// Gets the escape sequence which erases the entire line, followed by a line terminator.
    /// </summary>
    public static ReadOnlySpan<char> EraseEntireLineAndNewLineSpan => Environment.NewLine == "\r\n" ? "[2K\r\n" : "[2K\n";

    /// <summary>
    /// Gets the escape sequence which resets all the display attributes.
    /// </summary>
    public static ReadOnlySpan<char> ResetAttributesSpan => "[0m";

    /// <summary>
    /// Gets the escape sequence which saves the cursor position.
    /// </summary>
    public static ReadOnlySpan<char> SaveCursorSpan => "[s";

    /// <summary>
    /// Gets the escape sequence which restores the saved cursor position.
    /// </summary>
    public static ReadOnlySpan<char> RestoreCursorSpan => "[u";

    /// <summary>
    /// Gets the escape sequence which hides the cursor.
    /// </summary>
    public static ReadOnlySpan<char> HideCursorSpan => "[?25l";

    /// <summary>
    /// Gets the escape sequence which shows the cursor.
    /// </summary>
    public static ReadOnlySpan<char> ShowCursorSpan => "[?25h";

    /// <summary>
    /// Gets the prefix of the cursor position sequence (row and column follow).
    /// </summary>
    public static ReadOnlySpan<char> CursorPositionPrefixSpan => "["; // "\e[n;mH

    /// <summary>
    /// Gets the escape sequence which moves the cursor to the upper left corner.
    /// </summary>
    public static ReadOnlySpan<char> MoveCursorHomeSpan => "[0;0H";

    /// <summary>
    /// Gets the escape sequence which sets the specified foreground color.
    /// </summary>
    /// <param name="color">The foreground color.</param>
    /// <returns>The escape sequence (<see cref="DefaultForegroundColorEscapeCode"/> if the color is not supported).</returns>
    public static string GetForegroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "[30m",
            ConsoleColor.DarkRed => "[31m",
            ConsoleColor.DarkGray => "[90m",
            ConsoleColor.DarkGreen => "[32m",
            ConsoleColor.DarkYellow => "[33m",
            ConsoleColor.DarkBlue => "[34m",
            ConsoleColor.DarkMagenta => "[35m",
            ConsoleColor.DarkCyan => "[36m",
            ConsoleColor.Gray => "[37m",
            ConsoleColor.Red => "[1m[31m",
            ConsoleColor.Green => "[1m[32m",
            ConsoleColor.Yellow => "[1m[33m",
            ConsoleColor.Blue => "[1m[34m",
            ConsoleColor.Magenta => "[1m[35m",
            ConsoleColor.Cyan => "[1m[36m",
            ConsoleColor.White => "[1m[37m",
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
            ConsoleColor.Black => "[40m",
            ConsoleColor.DarkRed => "[41m",
            ConsoleColor.DarkGreen => "[42m",
            ConsoleColor.DarkYellow => "[43m",
            ConsoleColor.DarkBlue => "[44m",
            ConsoleColor.DarkMagenta => "[45m",
            ConsoleColor.DarkCyan => "[46m",
            ConsoleColor.Gray => "[47m",
            ConsoleColor.DarkGray => "[100m",
            ConsoleColor.Red => "[101m",
            ConsoleColor.Green => "[102m",
            ConsoleColor.Yellow => "[103m",
            ConsoleColor.Blue => "[104m",
            ConsoleColor.Magenta => "[105m",
            ConsoleColor.Cyan => "[106m",
            ConsoleColor.White => "[107m",
            _ => DefaultBackgroundColorEscapeCode,
        };
    }

    /// <summary>
    /// Converts an SGR parameter (30-37, 39, 90-97) into a foreground color.
    /// </summary>
    /// <param name="code">The SGR parameter of the escape sequence.</param>
    /// <param name="isBright"><see langword="true"/> if the bright (bold) attribute is set.</param>
    /// <param name="color">When this method returns, contains the color, or <see langword="null"/> for the default color.</param>
    /// <returns><see langword="true"/> if <paramref name="code"/> is a foreground color parameter.</returns>
    public static bool TryGetForegroundColor(int code, bool isBright, out ConsoleColor? color)
    {
        color = code switch
        {
            30 => isBright ? ConsoleColor.DarkGray : ConsoleColor.Black,
            31 => isBright ? ConsoleColor.Red : ConsoleColor.DarkRed,
            32 => isBright ? ConsoleColor.Green : ConsoleColor.DarkGreen,
            33 => isBright ? ConsoleColor.Yellow : ConsoleColor.DarkYellow,
            34 => isBright ? ConsoleColor.Blue : ConsoleColor.DarkBlue,
            35 => isBright ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta,
            36 => isBright ? ConsoleColor.Cyan : ConsoleColor.DarkCyan,
            37 => isBright ? ConsoleColor.White : ConsoleColor.Gray,
            90 => ConsoleColor.DarkGray,
            91 => ConsoleColor.Red,
            92 => ConsoleColor.Green,
            93 => ConsoleColor.Yellow,
            94 => ConsoleColor.Blue,
            95 => ConsoleColor.Magenta,
            96 => ConsoleColor.Cyan,
            97 => ConsoleColor.White,
            _ => null,
        };

        return color != null || code == 39;
    }

    /// <summary>
    /// Converts an SGR parameter (40-47, 49, 100-107) into a background color.
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
            100 => ConsoleColor.DarkGray,
            101 => ConsoleColor.Red,
            102 => ConsoleColor.Green,
            103 => ConsoleColor.Yellow,
            104 => ConsoleColor.Blue,
            105 => ConsoleColor.Magenta,
            106 => ConsoleColor.Cyan,
            107 => ConsoleColor.White,
            _ => null,
        };

        return color != null || code == 49;
    }
}
