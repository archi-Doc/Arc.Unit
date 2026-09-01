// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Holds the resolved output (and optional filter) for a pair of log source type and <see cref="LogLevel"/>.
/// </summary>
internal sealed class LogBroker
{
    public LogBroker(Type logSourceType, LogLevel logLevel, ILogOutput logOutput, ILogFilter? logFilter)
    {
        this.OutputType = logOutput.GetType();
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;

        // Delegates are created once per broker, and brokers are cached by LogUnit.
        this.LogDelegate = logOutput.Output;
        this.FilterDelegate = logFilter is null ? null : logFilter.Filter;
    }

    public Type OutputType { get; }

    public Type LogSourceType { get; }

    public LogLevel LogLevel { get; }

    public ILogOutput.OutputDelegate LogDelegate { get; }

    public ILogFilter.FilterDelegate? FilterDelegate { get; }
}
