// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Represents a pair of foreground and background console colors.
/// </summary>
public readonly record struct ConsoleColorPair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleColorPair"/> struct.
    /// </summary>
    /// <param name="foreground">The foreground color (<see cref="ConsoleHelper.DefaultColor"/> for the default color).</param>
    /// <param name="background">The background color (<see cref="ConsoleHelper.DefaultColor"/> for the default color).</param>
    public ConsoleColorPair(ConsoleColor foreground, ConsoleColor background)
    {
        this.Foreground = foreground;
        this.Background = background;
    }

    /// <summary>
    /// Gets the foreground color.
    /// </summary>
    public ConsoleColor Foreground { get; }

    /// <summary>
    /// Gets the background color.
    /// </summary>
    public ConsoleColor Background { get; }
}
