// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// <see cref="ILogOutput"/> which discards all logs.
/// </summary>
public class EmptyLogger : ILogOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyLogger"/> class.
    /// </summary>
    public EmptyLogger()
    {
    }

    /// <inheritdoc/>
    public void Output(LogEvent logEvent)
    {
    }
}
