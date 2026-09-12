// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Stores UTF-8 log lines in reusable circular storage. Oldest lines are evicted when the byte limit is exceeded.
/// </summary>
public class MemoryLogOutput : ILogOutput
{
    private readonly MemoryLogOutputOptions options;
    private readonly SimpleLogFormatter formatter;
    private readonly Lock syncObject = new();
    private readonly Queue<int> lengths = new();
    private System.Buffers.ArrayBufferWriter<byte> staging = new();
    private byte[] bytes = [];
    private int head;
    private int used;

    /// <summary>Initializes a new instance of the <see cref="MemoryLogOutput"/> class.</summary>
    /// <param name="options">Formatting and retained-byte limits.</param>
    public MemoryLogOutput(MemoryLogOutputOptions options)
    {
        this.options = options;
        this.formatter = new(options.FormatterOptions);
    }

    /// <inheritdoc/>
    public void Output(LogEvent logEvent)
    {
        lock (this.syncObject)
        {
            this.staging.ResetWrittenCount();
            try
            {
                var writer = new Utf8StringInterpolation.Utf8StringWriter<System.Buffers.ArrayBufferWriter<byte>>(this.staging);
                this.formatter.FormatUtf8(ref writer, logEvent);
                writer.Flush();
                var line = this.staging.WrittenSpan;
                var limit = this.options.MaxRetainedBytes <= 0 ? Array.MaxLength : Math.Min(this.options.MaxRetainedBytes, Array.MaxLength);
                while (this.used + (long)line.Length > limit && this.lengths.TryDequeue(out var length))
                {
                    this.head = (int)((this.head + (long)length) % this.bytes.Length);
                    this.used -= length;
                }

                if (line.Length > limit)
                {
                    return;
                }

                this.EnsureCapacity(this.used + line.Length);
                var tail = (int)((this.head + (long)this.used) % this.bytes.Length);
                var first = Math.Min(line.Length, this.bytes.Length - tail);
                line[..first].CopyTo(this.bytes.AsSpan(tail));
                line[first..].CopyTo(this.bytes);
                this.lengths.Enqueue(line.Length);
                this.used += line.Length;
            }
            finally
            {
                if (this.staging.Capacity > 1024 * 1024)
                {
                    this.staging = new();
                }
                else
                {
                    this.staging.ResetWrittenCount();
                }
            }
        }
    }

    /// <summary>Clears retained logs. Storage is reused by subsequent writes.</summary>
    public void Clear()
    {
        lock (this.syncObject)
        {
            this.lengths.Clear();
            this.head = 0;
            this.used = 0;
        }
    }

    /// <summary>Copies retained UTF-8 lines in input order.</summary>
    /// <returns>An independent byte array, or the shared empty array if no logs are retained.</returns>
    public byte[] ToUtf8Array()
    {
        lock (this.syncObject)
        {
            if (this.used == 0)
            {
                return Array.Empty<byte>();
            }

            var result = new byte[this.used];
            this.CopyTo(result);
            return result;
        }
    }

    private void EnsureCapacity(int required)
    {
        if (required <= this.bytes.Length)
        {
            return;
        }

        var capacity = (int)Math.Min(Array.MaxLength, Math.Max(required, Math.Max(256L, this.bytes.Length * 2L)));
        var replacement = new byte[capacity];
        this.CopyTo(replacement);
        this.bytes = replacement;
        this.head = 0;
    }

    private void CopyTo(Span<byte> destination)
    {
        var first = Math.Min(this.used, this.bytes.Length - this.head);
        this.bytes.AsSpan(this.head, first).CopyTo(destination);
        this.bytes.AsSpan(0, this.used - first).CopyTo(destination[first..]);
    }
}
