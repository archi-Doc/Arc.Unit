// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Interface for filtering log events.<br/>
/// A filter is applied before the log is passed to <see cref="ILogOutput"/>, and it can change the destination or discard the log.
/// </summary>
public interface ILogFilter
{
    internal delegate LogWriter? FilterDelegate(LogFilterParameter parameter);

    /// <summary>
    /// Determines the <see cref="LogWriter"/> which actually writes the log.
    /// </summary>
    /// <param name="parameter">The information of the log to be written.</param>
    /// <returns>
    /// <see cref="LogFilterParameter.OriginalWriter"/> to keep the original destination,<br/>
    /// another <see cref="LogWriter"/> to change the destination,<br/>
    /// or <see langword="null"/> to discard the log.
    /// </returns>
    public LogWriter? Filter(LogFilterParameter parameter);
}
