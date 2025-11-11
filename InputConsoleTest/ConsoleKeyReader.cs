// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Threading;
using ConsoleBufferTest;

namespace Arc.InputConsole;

internal sealed class ConsoleKeyReader
{
    private readonly Task task;
    // private readonly Thread thread;
    private readonly ConcurrentQueue<ConsoleKeyInfo> queue =
        new();

    private byte posixDisableValue;
    private byte veraseCharacter;

    public ConsoleKeyReader(CancellationToken cancellationToken = default)
    {
        this.Initialize();

        this.task = new Task(
            () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var keyInfo = Console.ReadKey(intercept: true);
                        this.queue.Enqueue(keyInfo);
                    }
                    catch
                    {
                        Thread.Sleep(10);
                    }
                }
            },
            cancellationToken,
            TaskCreationOptions.LongRunning);

        this.task.Start();

        /*this.thread = new Thread(new ParameterizedThreadStart(Process));
        this.thread.Start(this);*/
    }

    private void Initialize()
    {
        try
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

            // s_veolCharacter = controlCharacterValues[1];
            // s_veol2Character = controlCharacterValues[2];
            //s_veofCharacter = controlCharacterValues[3];
        }
        catch
        { }
    }

    public bool TryRead(out ConsoleKeyInfo keyInfo)
    {
        return this.queue.TryDequeue(out keyInfo);
    }

    private static void Process(object? obj)
    {
        var reader = (ConsoleKeyReader)obj!;
        while (true)
        {
            try
            {
                var keyInfo = Console.ReadKey(intercept: true);
                reader.queue.Enqueue(keyInfo);
            }
            catch
            {
                Thread.Sleep(10);
            }
        }
    }

    public bool IsKeyAvailable => !this.queue.IsEmpty;
}
