// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace Arc.Unit;

public class MemoryLogger : ILogOutput
{
    public MemoryLogger(MemoryLoggerOptions options)
    {
        this.options = options;
        this.formatter = new(this.options.Formatter);
    }

    private readonly MemoryLoggerOptions options;
    private readonly SimpleLogFormatter formatter;

    private readonly object syncObject = new();
    private readonly Queue<byte[]> queue = new();
    private long memoryUsage;

    public void Output(LogEvent param)
    {
        var b = this.formatter.FormatUtf8(param);

        lock (this.syncObject)
        {
            this.queue.Enqueue(b);
            this.memoryUsage += b.Length;

            while (this.memoryUsage > this.options.MaxMemoryUsage)
            {
                this.queue.TryDequeue(out var b2);
                if (b2 is null)
                {
                    break;
                }

                this.memoryUsage -= b2.Length;
            }
        }
    }

    public byte[] ToArray()
    {
        lock (this.syncObject)
        {
            var memory = new byte[this.memoryUsage];
            var span = memory.AsSpan();

            foreach (var x in this.queue)
            {
                x.AsSpan().CopyTo(span);
                span = span.Slice(x.Length);
            }

            return memory;
        }
    }
}
