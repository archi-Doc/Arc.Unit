// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Interface for receiving and outputting log events.
/// </summary>
public interface ILogOutput
{
    internal delegate void OutputDelegate(LogEvent param);

    public void Output(LogEvent param);
}
