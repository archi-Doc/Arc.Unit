// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class ConsoleService : IConsoleService
{
    public ConsoleService()
    {
    }

    public void Write(string? message = null)
    {
        try
        {
            Console.Out.Write(message);
        }
        catch
        {
        }
    }

    public void WriteLine(string? message = null)
    {
        try
        {
            Console.Out.WriteLine(message);
        }
        catch
        {
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
}
