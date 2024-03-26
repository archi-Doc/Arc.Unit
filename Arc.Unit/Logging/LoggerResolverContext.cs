// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

public sealed class LoggerResolverContext
{
    public LoggerResolverContext(UnitLogger unitLogger, LogSourceLevelPair pair)
    {
        this.unitLogger = unitLogger;
        this.LogSourceType = pair.LogSourceType;
        this.LogLevel = pair.LogLevel;
    }

    /*public LoggerResolverContext(Type logSourceType, LogLevel logLevel)
    {
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;
    }*/

    public void SetOutput<TLogOutput>()
        where TLogOutput : ILogOutput
    {
        this.LogOutputType = typeof(TLogOutput);
    }

    public void SetOutputType(Type logOutputType)
    {
        if (!logOutputType.GetInterfaces().Contains(typeof(ILogOutput)))
        {
            throw new ArgumentException($"{nameof(logOutputType)} must implement {nameof(ILogOutput)} interface.");
        }

        this.LogOutputType = logOutputType;
    }

    public void SetFilter<TLogFilter>()
        where TLogFilter : ILogFilter
    {
        this.LogFilterType = typeof(TLogFilter);
    }

    public void SetFilterType(Type logFilterType)
    {
        if (!logFilterType.GetInterfaces().Contains(typeof(ILogFilter)))
        {
            throw new ArgumentException($"{nameof(logFilterType)} must implement {nameof(ILogFilter)} interface.");
        }

        this.LogFilterType = logFilterType;
    }

    public void SetOutputAndFilter<TLogOutput, TLogFilter>()
        where TLogOutput : ILogOutput
        where TLogFilter : ILogFilter
    {
        this.LogOutputType = typeof(TLogOutput);
        this.LogFilterType = typeof(TLogFilter);
    }

    public void TrySetOutput<TLogOutput>()
        where TLogOutput : ILogOutput
    {
        if (this.LogOutputType == null)
        {
            this.LogOutputType = typeof(TLogOutput);
        }
    }

    public void TrySetFilter<TLogFilter>()
        where TLogFilter : ILogFilter
    {
        if (this.LogFilterType == null)
        {
            this.LogFilterType = typeof(TLogFilter);
        }
    }

    public void TrySetOutputAndFilter<TLogOutput, TLogFilter>()
        where TLogOutput : ILogOutput
        where TLogFilter : ILogFilter
    {
        if (this.LogOutputType == null)
        {
            this.LogOutputType = typeof(TLogOutput);
        }

        if (this.LogFilterType == null)
        {
            this.LogFilterType = typeof(TLogFilter);
        }
    }

    public void ClearOutput()
    {
        this.LogOutputType = null;
    }

    public void ClearFilter()
    {
        this.LogFilterType = null;
    }

    public void ClearOutputAndFilter()
    {
        this.LogOutputType = null;
        this.LogFilterType = null;
    }

    public void GetOptions<TOptions>(out TOptions options)
        where TOptions : class
        => options = this.unitLogger.UnitContext.ServiceProvider.GetRequiredService<TOptions>();

    public bool TryGetOptions<TOptions>([MaybeNullWhen(false)] out TOptions options)
        where TOptions : class
    {
        options = this.unitLogger.UnitContext.ServiceProvider.GetService<TOptions>();
        return options != null;
    }

    public Type LogSourceType { get; }

    public LogLevel LogLevel { get; }

    public Type? LogOutputType { get; private set; }

    public Type? LogFilterType { get; private set; }

    private UnitLogger unitLogger;
}
