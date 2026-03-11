// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public readonly record struct LogWriter
{
    internal LogWriter(ILogService logService, LogBroker logBroker)
    {
        this.logService = logService;
        this.logBroker = logBroker;
    }

    public void Log(string message, long eventId = default)
    {
        var broker = this.logBroker;
        LogEvent param = new(this.logService, broker.LogSourceType, broker.LogLevel, eventId, message);
        if (broker.FilterDelegate is not null)
        {// Filter -> Log
            if (broker.FilterDelegate(new(this.logService, broker.LogSourceType, broker.LogLevel, eventId, this)) is LogWriter loggerInstance)
            {
                loggerInstance.logBroker.LogDelegate(new(this.logService, broker.LogSourceType, loggerInstance.logBroker.LogLevel, eventId, message));
            }
        }
        else
        {// Log
            broker.LogDelegate(param);
        }
    }

    private readonly ILogService logService;
    private readonly LogBroker logBroker;

    public Type OutputType => this.logBroker.OutputType;
}
