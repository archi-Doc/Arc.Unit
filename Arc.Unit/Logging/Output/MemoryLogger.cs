// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// <see cref="ILogOutput"/> which keeps the formatted logs (UTF-8) in memory.<br/>
/// The oldest logs are discarded when the memory usage exceeds <see cref="MemoryLoggerOptions.MaxMemoryUsage"/>.
/// </summary>
public class MemoryLogger : ILogOutput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryLogger"/> class.
    /// </summary>
    /// <param name="options"><see cref="MemoryLoggerOptions"/>.</param>
    public MemoryLogger(MemoryLoggerOptions options)
    {
        this.options = options;
        this.formatter = new(this.options.FormatterOptions);
    }

    private readonly MemoryLoggerOptions options;
    private readonly SimpleLogFormatter formatter;

    private readonly object syncObject = new();
    private readonly Queue<byte[]> queue = new();
    private long memoryUsage;

    /// <inheritdoc/>
    public void Output(LogEvent logEvent)
    {
        var b = this.formatter.FormatUtf8(logEvent);
        var maxMemoryUsage = this.options.MaxMemoryUsage;

        lock (this.syncObject)
        {
            this.queue.Enqueue(b);
            this.memoryUsage += b.Length;

            while (maxMemoryUsage > 0 && this.memoryUsage > maxMemoryUsage)
            {// 0: unlimited
                if (!this.queue.TryDequeue(out var b2))
                {
                    break;
                }

                this.memoryUsage -= b2.Length;
            }
        }
    }

    /// <summary>
    /// Removes all the logs kept in memory.
    /// </summary>
    public void Clear()
    {
        lock (this.syncObject)
        {
            this.queue.Clear();
            this.memoryUsage = 0;
        }
    }

    /// <summary>
    /// Copies all the logs kept in memory into a byte array.
    /// </summary>
    /// <returns>The UTF-8 encoded logs.</returns>
    public byte[] ToUtf8Array()
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
