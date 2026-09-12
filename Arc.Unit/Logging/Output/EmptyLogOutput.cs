// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// <see cref="ILogOutput"/> which discards all logs.
/// </summary>
public class EmptyLogOutput : ILogOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyLogOutput"/> class.
    /// </summary>
    public EmptyLogOutput()
    {
    }

    /// <inheritdoc/>
    public void Output(LogEvent logEvent)
    {
    }
}
