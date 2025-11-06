// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public readonly struct ConsoleColorPair
{
    public ConsoleColorPair(ConsoleColor foreground, ConsoleColor background)
    {
        this.Foreground = foreground;
        this.Background = background;
    }

    public ConsoleColor Foreground { get; }

    public ConsoleColor Background { get; }
}
