// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Arc.Unit;

internal class LogBroker
{
    public LogBroker(Type logSourceType, LogLevel logLevel, ILogOutput logOutput, ILogFilter? logFilter)
    {
        this.OutputType = logOutput.GetType();
        this.LogSourceType = logSourceType;
        this.LogLevel = logLevel;

        this.LogDelegate = (ILogOutput.OutputDelegate)delegateCache.GetOrAdd(logOutput, static x =>
        {
            var type = x.GetType();
            var method = type.GetMethod(nameof(ILogOutput.Output));
            if (method == null)
            {
                throw new ArgumentException();
            }

            return Delegate.CreateDelegate(typeof(ILogOutput.OutputDelegate), x, method);
        });

        if (logFilter is not null)
        {
            this.FilterDelegate = (ILogFilter.FilterDelegate)delegateCache.GetOrAdd(logFilter, static x =>
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

    private static ConcurrentDictionary<object, Delegate> delegateCache = new();

    public Type OutputType { get; }

    public Type LogSourceType { get; }

    public LogLevel LogLevel { get; }

    public ILogOutput.OutputDelegate LogDelegate { get; }

    public ILogFilter.FilterDelegate? FilterDelegate { get; }
}
