// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using ConsoleBufferTest;

namespace Arc.InputConsole;

internal sealed class ConsoleKeyReader
{
    private readonly Task task;
    private readonly ConcurrentQueue<ConsoleKeyInfo> queue =
        new();

    private bool enableStdin;
    private byte posixDisableValue;
    private byte veraseCharacter;

    public ConsoleKeyReader(CancellationToken cancellationToken = default)
    {
        try
        {
            this.InitializeStdIn();
            Console.WriteLine("StdIn");
        }
        catch
        {
            Console.WriteLine("No StdIn");
        }

        this.task = new Task(
            async () =>
            {
                var bufPtr = new byte[100];
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (this.enableStdin)
                        {// StdIn
                            /*if (!Interop.Sys.StdinReady())
                            {
                                await Task.Delay(10);
                                continue;
                            }

                            this.StdIn();*/

                            var keyInfo = Console.ReadKey(intercept: true);
                            this.queue.Enqueue(keyInfo);
                        }
                        else
                        {// Console.ReadKey
                            var keyInfo = Console.ReadKey(intercept: true);
                            this.queue.Enqueue(keyInfo);
                        }
                    }
                    catch
                    {
                        await Task.Delay(10);
                    }
                }
            },
            cancellationToken,
            TaskCreationOptions.LongRunning);

        this.task.Start();
    }

    private unsafe void StdIn()
    {
        var buffer = new byte[100];
        Interop.Sys.InitializeConsoleBeforeRead();
        try
        {
            fixed (byte* ptr = buffer)
            {
                int result = Interop.Sys.ReadStdin(ptr, 100);
                // Console.WriteLine(result);
                // Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer, result));
                // Console.WriteLine(BitConverter.ToString(bufPtr.Slice(0, result).ToArray()));
            }
        }
        finally
        {
            Interop.Sys.UninitializeConsoleAfterRead();
        }

        var keyInfo = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
        this.queue.Enqueue(keyInfo);
    }

    public bool TryRead(out ConsoleKeyInfo keyInfo)
    {
        return this.queue.TryDequeue(out keyInfo);
    }

    private void InitializeStdIn()
    {
        const int NumControlCharacterNames = 4;
        Span<Interop.ControlCharacterNames> controlCharacterNames = stackalloc Interop.ControlCharacterNames[NumControlCharacterNames]
        {
            Interop.ControlCharacterNames.VERASE,
            Interop.ControlCharacterNames.VEOL,
            Interop.ControlCharacterNames.VEOL2,
            Interop.ControlCharacterNames.VEOF,
        };

        Span<byte> controlCharacterValues = stackalloc byte[NumControlCharacterNames];
        Interop.Sys.GetControlCharacters(controlCharacterNames, controlCharacterValues, NumControlCharacterNames, out var posixDisableValue);
        this.posixDisableValue = posixDisableValue;
        this.veraseCharacter = controlCharacterValues[0];

        Console.WriteLine(this.posixDisableValue);
        Console.WriteLine(this.veraseCharacter);

        Interop.Sys.InitializeConsoleBeforeRead();
        Interop.Sys.UninitializeConsoleAfterRead();

        this.enableStdin = true;
    }
}
