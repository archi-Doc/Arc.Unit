// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Provides the log source/level to <see cref="LoggerResolverDelegate"/>, and receives the resolved output and filter.
/// </summary>
public sealed class LoggerResolverContext
{
    internal LoggerResolverContext(LogSourceLevelPair pair)
    {
        this.LogSourceType = pair.LogSourceType;
        this.LogLevel = pair.LogLevel;
    }

    /// <summary>
    /// Gets the log source type to be resolved (the category of the log).
    /// </summary>
    public Type LogSourceType { get; }

    /// <summary>
    /// Gets the log level to be resolved.
    /// </summary>
    public LogLevel LogLevel { get; }

    /// <summary>
    /// Gets the resolved log output type (<see langword="null"/>: no log is written).
    /// </summary>
    public Type? LogOutputType { get; private set; }

    /// <summary>
    /// Gets the resolved log filter type (<see langword="null"/>: no filter is applied).
    /// </summary>
    public Type? LogFilterType { get; private set; }

    /// <summary>
    /// Sets the log output.
    /// </summary>
    /// <typeparam name="TLogOutput">The type of <see cref="ILogOutput"/>.</typeparam>
    public void SetOutput<TLogOutput>()
        where TLogOutput : ILogOutput
    {
        this.LogOutputType = typeof(TLogOutput);
    }

    /// <summary>
    /// Sets the log output.
    /// </summary>
    /// <param name="logOutputType">The type which implements <see cref="ILogOutput"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="logOutputType"/> does not implement <see cref="ILogOutput"/>.</exception>
    public void SetOutputType(Type logOutputType)
    {
        if (!typeof(ILogOutput).IsAssignableFrom(logOutputType))
        {
            throw new ArgumentException($"{nameof(logOutputType)} must implement {nameof(ILogOutput)} interface.");
        }

        this.LogOutputType = logOutputType;
    }

    /// <summary>
    /// Sets the log filter.
    /// </summary>
    /// <typeparam name="TLogFilter">The type of <see cref="ILogFilter"/>.</typeparam>
    public void SetFilter<TLogFilter>()
        where TLogFilter : ILogFilter
    {
        this.LogFilterType = typeof(TLogFilter);
    }

    /// <summary>
    /// Sets the log filter.
    /// </summary>
    /// <param name="logFilterType">The type which implements <see cref="ILogFilter"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="logFilterType"/> does not implement <see cref="ILogFilter"/>.</exception>
    public void SetFilterType(Type logFilterType)
    {
        if (!typeof(ILogFilter).IsAssignableFrom(logFilterType))
        {
            throw new ArgumentException($"{nameof(logFilterType)} must implement {nameof(ILogFilter)} interface.");
        }

        this.LogFilterType = logFilterType;
    }

    /// <summary>
    /// Sets the log output and the log filter.
    /// </summary>
    /// <typeparam name="TLogOutput">The type of <see cref="ILogOutput"/>.</typeparam>
    /// <typeparam name="TLogFilter">The type of <see cref="ILogFilter"/>.</typeparam>
    public void SetOutputAndFilter<TLogOutput, TLogFilter>()
        where TLogOutput : ILogOutput
        where TLogFilter : ILogFilter
    {
        this.LogOutputType = typeof(TLogOutput);
        this.LogFilterType = typeof(TLogFilter);
    }

    /// <summary>
    /// Sets the log output if it has not been set yet.
    /// </summary>
    /// <typeparam name="TLogOutput">The type of <see cref="ILogOutput"/>.</typeparam>
    public void TrySetOutput<TLogOutput>()
        where TLogOutput : ILogOutput
    {
        this.LogOutputType ??= typeof(TLogOutput);
    }

    /// <summary>
    /// Sets the log filter if it has not been set yet.
    /// </summary>
    /// <typeparam name="TLogFilter">The type of <see cref="ILogFilter"/>.</typeparam>
    public void TrySetFilter<TLogFilter>()
        where TLogFilter : ILogFilter
    {
        this.LogFilterType ??= typeof(TLogFilter);
    }

    /// <summary>
    /// Sets the log output and the log filter if they have not been set yet.
    /// </summary>
    /// <typeparam name="TLogOutput">The type of <see cref="ILogOutput"/>.</typeparam>
    /// <typeparam name="TLogFilter">The type of <see cref="ILogFilter"/>.</typeparam>
    public void TrySetOutputAndFilter<TLogOutput, TLogFilter>()
        where TLogOutput : ILogOutput
        where TLogFilter : ILogFilter
    {
        this.LogOutputType ??= typeof(TLogOutput);
        this.LogFilterType ??= typeof(TLogFilter);
    }

    /// <summary>
    /// Clears the log output (no log is written).
    /// </summary>
    public void ClearOutput()
    {
        this.LogOutputType = null;
    }

    /// <summary>
    /// Clears the log filter.
    /// </summary>
    public void ClearFilter()
    {
        this.LogFilterType = null;
    }

    /// <summary>
    /// Clears the log output and the log filter.
    /// </summary>
    public void ClearOutputAndFilter()
    {
        this.LogOutputType = null;
        this.LogFilterType = null;
    }
}
