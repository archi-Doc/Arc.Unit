﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Arc.Unit;

internal class LogInstance : ILogWriter
{
    public LogInstance(ILogContext context, Type logSourceType, LogLevel logLevel, ILogOutput logOutput, ILogFilter? logFilter)
    {
        this.context = context;
        this.OutputType = logOutput.GetType();
        this.logSourceType = logSourceType;
        this.logLevel = logLevel;

        this.logDelegate = (ILogOutput.OutputDelegate)delegateCache.GetOrAdd(logOutput, static x =>
        {
            var type = x.GetType();
            var method = type.GetMethod(nameof(ILogOutput.Output));
            if (method == null)
            {
                throw new ArgumentException();
            }

            return Delegate.CreateDelegate(typeof(ILogOutput.OutputDelegate), x, method);
        });

        if (logFilter != null)
        {
            this.filterDelegate = (ILogFilter.FilterDelegate)delegateCache.GetOrAdd(logFilter, static x =>
            {
                var type = x.GetType();
                var method = type.GetMethod(nameof(ILogFilter.Filter));
                if (method == null)
                {
                    throw new ArgumentException();
                }

                return Delegate.CreateDelegate(typeof(ILogFilter.FilterDelegate), x, method);
            });
        }
    }

    public void Log(long eventId, string message, Exception? exception)
    {
        LogEvent param = new(this.logSourceType, this.logLevel, eventId, message, exception);
        if (this.filterDelegate != null)
        {// Filter -> Log
            if (this.filterDelegate(new(this.context, this.logSourceType, this.logLevel, eventId, this)) is LogInstance loggerInstance)
            {
                loggerInstance.logDelegate(new(this.logSourceType, loggerInstance.logLevel, eventId, message, exception));
            }
        }
        else
        {// Log
            this.logDelegate(param);
        }
    }

    private static ConcurrentDictionary<object, Delegate> delegateCache = new();

    public Type OutputType { get; }

    private ILogContext context;
    private Type logSourceType;
    private LogLevel logLevel;
    private ILogOutput.OutputDelegate logDelegate;
    private ILogFilter.FilterDelegate? filterDelegate;
}
