// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;

namespace Arc.Unit;

public class ConsoleService : IConsoleService
{
    private const int StackallocThreshold = 1024;
    private const int BufferMargin = 16;

    public ConsoleService()
    {
    }

    public void Write(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
        else if (!this.EnableColor || color == ConsoleHelper.DefaultColor)
        {
            try
            {
                Console.Out.Write(message);
            }
            catch
            {
            }

            return;
        }

        var length = message.Length + BufferMargin;
        char[]? rent = null;
        Span<char> buffer = length <= StackallocThreshold ?
            stackalloc char[length] : (rent = ArrayPool<char>.Shared.Rent(length));

        var destination = buffer;
        var source = ConsoleHelper.GetForegroundColorEscapeCode(color).AsSpan();
        source.CopyTo(destination);
        destination = destination.Slice(source.Length);
        message.AsSpan().CopyTo(destination);
        destination = destination.Slice(message.Length);
        source = ConsoleHelper.ResetSpan;
        source.CopyTo(destination);

        try
        {
            Console.Out.Write(buffer);
        }
        catch
        {
        }
        finally
        {
            if (rent is not null)
            {
                ArrayPool<char>.Shared.Return(rent);
            }
        }
    }

    public void WriteLine(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor)
    {
        if (string.IsNullOrEmpty(message) || !this.EnableColor || color == ConsoleHelper.DefaultColor)
        {
            try
            {
                Console.Out.WriteLine(message);
            }
            catch
            {
            }

            return;
        }

        var length = message.Length + BufferMargin;
        char[]? rent = null;
        Span<char> buffer = length <= StackallocThreshold ?
            stackalloc char[length] : (rent = ArrayPool<char>.Shared.Rent(length));

        var destination = buffer;
        var source = ConsoleHelper.GetForegroundColorEscapeCode(color).AsSpan();
        source.CopyTo(destination);
        destination = destination.Slice(source.Length);
        message.AsSpan().CopyTo(destination);
        destination = destination.Slice(message.Length);
        source = ConsoleHelper.ResetSpan;
        source.CopyTo(destination);

        try
        {
            Console.Out.WriteLine(buffer);
        }
        catch
        {
        }
        finally
        {
            if (rent is not null)
            {
                ArrayPool<char>.Shared.Return(rent);
            }
        }
    }

    public async Task<InputResult> ReadLine(CancellationToken cancellationToken)
    {
        try
        {
            try
            {
                var text = await Console.In.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                return new(text ?? string.Empty);
            }
            catch (OperationCanceledException)
            {
                return new(InputResultKind.Canceled);
            }
        }
        catch
        {
            return new(InputResultKind.Terminated);
        }
    }

    public ConsoleKeyInfo ReadKey(bool intercept)
    {
        try
        {
            return Console.ReadKey();
        }
        catch
        {
            return default;
        }
    }

    public bool KeyAvailable
    {
        get
        {
            try
            {
                return Console.KeyAvailable;
            }
            catch
            {
                return false;
            }
        }
    }

    public bool EnableColor { get; set; } = true;
}
