// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

public class MemoryLogger : ILogOutput
{
    public MemoryLogger(MemoryLoggerOptions options)
    {
        this.options = options;
    }

    public void Output(LogEvent param)
    {
    }

    private readonly MemoryLoggerOptions options;
}
