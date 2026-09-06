// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Reuses queue storage and atomically enforces capacity and completion across producers.
/// </summary>
internal sealed class LogEventQueue
{
    private readonly Lock syncObject = new();
    private readonly Queue<LogEvent> queue = new();
    private bool completed;

    internal int Count
    {
        get
        {
            lock (this.syncObject)
            {
                return this.queue.Count;
            }
        }
    }

    internal void Enqueue(LogEvent logEvent, int maxQueue)
    {
        lock (this.syncObject)
        {
            if (!this.completed && (maxQueue <= 0 || this.queue.Count < maxQueue))
            {
                this.queue.Enqueue(logEvent);
            }
        }
    }

    internal bool TryDequeue(out LogEvent logEvent)
    {
        lock (this.syncObject)
        {
            return this.queue.TryDequeue(out logEvent);
        }
    }

    internal void Complete()
    {
        lock (this.syncObject)
        {
            this.completed = true;
        }
    }
}
