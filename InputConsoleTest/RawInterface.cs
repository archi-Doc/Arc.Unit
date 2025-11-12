// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleBufferTest;

namespace Arc.InputConsole;

internal sealed class RawInterface
{
    private readonly InputConsole inputConsole;
    private SafeHandle? handle;
    private bool enableStdin;
    private byte posixDisableValue;
    private byte veraseCharacter;

    public RawInterface(InputConsole inputConsole, CancellationToken cancellationToken = default)
    {
        this.inputConsole = inputConsole;

        try
        {
            this.InitializeStdIn();
            Console.WriteLine("StdIn");
        }
        catch
        {
            Console.WriteLine("No StdIn");
        }
    }

    public unsafe bool TryRead(out ConsoleKeyInfo keyInfo)
    {
        try
        {
            if (this.enableStdin)
            {// StdIn
                // Peek
                if (!Interop.Sys.StdinReady())
                {
                    keyInfo = default;
                    return false;
                }

                Interop.Sys.InitializeConsoleBeforeRead();
                try
                {
                    Span<byte> bufPtr = stackalloc byte[100];
                    fixed (byte* buffer = bufPtr)
                    {
                        int result = Interop.Sys.ReadStdin(buffer, 100);
                        // Console.Write(result);
                        // Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer, result));
                        // Console.WriteLine(BitConverter.ToString(bufPtr.Slice(0, result).ToArray()));
                    }
                }
                finally
                {
                    Interop.Sys.UninitializeConsoleAfterRead();
                }

                keyInfo = new('a', ConsoleKey.A, false, false, false);
                return true;
            }
            else
            {// Console.ReadKey
                // Peek
                if (!Console.KeyAvailable)
                {
                    keyInfo = default;
                    return false;
                }

                keyInfo = Console.ReadKey(intercept: true);
                return true;
            }
        }
        catch
        {
            keyInfo = default;
            return false;
        }
    }

    public unsafe void WriteRaw(ReadOnlySpan<char> data)
    {
        if (this.handle is not null)
        {
            var length = Encoding.UTF8.GetBytes(data, this.inputConsole.Utf8Buffer);
            fixed (byte* p = this.inputConsole.Utf8Buffer)
            {
                _ = Interop.Sys.Write(this.handle, p, length);
            }
        }
        else
        {
            Console.Out.Write(data);
        }
    }

    private void InitializeStdIn()
    {
        this.handle = Interop.Sys.Dup(Interop.FileDescriptors.STDIN_FILENO);

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
