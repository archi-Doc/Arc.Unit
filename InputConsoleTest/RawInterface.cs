// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleBufferTest;

namespace Arc.InputConsole;

internal sealed class RawInterface
{
    private const int BufferCapacity = 26; // 1024
    private const int MinimalSequenceLength = 3;
    private const char Escape = '\e';
    private const char Delete = '\u007F';

    private readonly InputConsole inputConsole;
    private readonly Encoding encoding;

    private readonly Lock bufferLock = new();
    private readonly byte[] bytes = new byte[BufferCapacity];
    private readonly char[] chars = new char[BufferCapacity];
    // private int bytesPosition = 0;
    private int bytesLength = 0;
    private int charsPosition = 0;
    private int charsLength = 0;

    private SafeHandle? handle;
    private bool enableStdin;
    private byte posixDisableValue;
    private byte veraseCharacter;

    // public Span<byte> BytesSpan => this.bytes.AsSpan(this.bytesPosition, this.bytesLength);

    public Span<char> CharsSpan => this.chars.AsSpan(this.charsPosition, this.charsLength);

    public bool IsBytesEmpty => this.bytesLength == 0;

    public bool IsCharsEmpty => this.charsLength == 0;

    public RawInterface(InputConsole inputConsole, CancellationToken cancellationToken = default)
    {
        this.inputConsole = inputConsole;
        this.encoding = Encoding.UTF8;

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
                if (this.TryConsumeBuffer(out keyInfo))
                {
                    return true;
                }

                // Peek
                if (!Interop.Sys.StdinReady())
                {
                    keyInfo = default;
                    return false;
                }

                using (this.bufferLock.EnterScope())
                {
                    if (this.TryConsumeBufferInternal(out keyInfo))
                    {
                        return true;
                    }

                    Interop.Sys.InitializeConsoleBeforeRead();
                    try
                    {
                        var span = this.bytes.AsSpan(this.bytesLength, this.bytes.Length - this.bytesLength);
                        fixed (byte* buffer = span)
                        {
                            var readLength = Interop.Sys.ReadStdin(buffer, span.Length);
                            this.bytesLength += readLength;

                            // Console.Write(result);
                            // Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer, result));
                            // Console.WriteLine(BitConverter.ToString(bufPtr.Slice(0, result).ToArray()));
                        }

                        var validLength = InputConsoleHelper.GetValidUtf8Length(this.bytes.AsSpan(0, this.bytesLength));

                        Debug.Assert(this.IsCharsEmpty);
                        this.charsPosition = 0;
                        this.charsLength = this.encoding.GetChars(this.bytes.AsSpan(0, validLength), this.chars.AsSpan());
                        this.bytesLength -= validLength;
                        if (validLength < this.bytesLength)
                        {
                            // Move remaining bytes to the front
                            this.bytes.AsSpan(validLength, this.bytesLength).CopyTo(this.bytes.AsSpan());
                        }
                    }
                    finally
                    {
                        Interop.Sys.UninitializeConsoleAfterRead();
                    }

                    // keyInfo = new('a', ConsoleKey.A, false, false, false);
                    return this.TryConsumeBufferInternal(out keyInfo);
                }
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

    private static ConsoleKeyInfo ParseFromSingleChar(char single, bool isAlt)
    {
        bool isShift = false, isCtrl = false;
        char keyChar = single;

        ConsoleKey key = single switch
        {
            '\b' => ConsoleKey.Backspace,
            '\t' => ConsoleKey.Tab,
            '\r' or '\n' => ConsoleKey.Enter,
            ' ' => ConsoleKey.Spacebar,
            Escape => ConsoleKey.Escape,
            Delete => ConsoleKey.Backspace,
            '*' => ConsoleKey.Multiply,
            '/' => ConsoleKey.Divide,
            '-' => ConsoleKey.Subtract,
            '+' => ConsoleKey.Add,
            '=' => default,
            '!' or '@' or '#' or '$' or '%' or '^' or '&' or '&' or '*' or '(' or ')' => default,
            ',' => ConsoleKey.OemComma,
            '.' => ConsoleKey.OemPeriod,
            _ when char.IsAsciiLetterLower(single) => ConsoleKey.A + single - 'a',
            _ when char.IsAsciiLetterUpper(single) => UppercaseCharacter(single, out isShift),
            _ when char.IsAsciiDigit(single) => ConsoleKey.D0 + single - '0',
            _ when char.IsBetween(single, (char)1, (char)26) => ControlAndLetterPressed(single, isAlt, out keyChar, out isCtrl),
            _ when char.IsBetween(single, (char)28, (char)31) => ControlAndDigitPressed(single, out keyChar, out isCtrl),
            '\u0000' => ControlAndDigitPressed(single, out keyChar, out isCtrl),
            _ => default,
        };

        if (single is '\b' or '\n')
        {
            isCtrl = true;
        }

        if (isAlt)
        {
            isAlt = key != default;
        }

        return new ConsoleKeyInfo(keyChar, key, isShift, isAlt, isCtrl);

        static ConsoleKey UppercaseCharacter(char single, out bool isShift)
        {
            isShift = true;
            return ConsoleKey.A + single - 'A';
        }

        static ConsoleKey ControlAndLetterPressed(char single, bool isAlt, out char keyChar, out bool isCtrl)
        {
            Debug.Assert(single != 'b' && single != '\t' && single != '\n' && single != '\r');

            isCtrl = true;
            keyChar = isAlt ? default : single;
            return ConsoleKey.A + single - 1;
        }

        static ConsoleKey ControlAndDigitPressed(char single, out char keyChar, out bool isCtrl)
        {
            Debug.Assert(single == default || char.IsBetween(single, (char)28, (char)31));

            isCtrl = true;
            keyChar = default;
            return single switch
            {
                '\u0000' => ConsoleKey.D2,
                _ => ConsoleKey.D4 + single - 28,
            };
        }
    }

    private bool TryConsumeBuffer(out ConsoleKeyInfo keyInfo)
    {
        if (this.IsCharsEmpty)
        {
            keyInfo = default;
            return false;
        }

        using (this.bufferLock.EnterScope())
        {
            return this.TryConsumeBufferInternal(out keyInfo);
        }
    }

    private bool TryConsumeBufferInternal(out ConsoleKeyInfo keyInfo)
    {
        if (this.IsCharsEmpty)
        {
            keyInfo = default;
            return false;
        }

        keyInfo = default;
        return false;

        /*var span = this.BufferSpan;
        if (span[0] != this.posixDisableValue && span[0] == this.veraseCharacter)
        {
            keyInfo = new(span[0], ConsoleKey.Backspace, false, false, false);
            this.bufferPosition++;
            this.bufferLength--;
            return true;
        }
        else if (span.Length >= MinimalSequenceLength + 1 && span[0] == Escape && span[1] == Escape)
        {
            startIndex++;
            if (TryParseTerminalInputSequence(buffer, terminalFormatStrings, out ConsoleKeyInfo parsed, ref startIndex, endIndex))
            {
                keyInfo = new(parsed.KeyChar, parsed.Key, (parsed.Modifiers & ConsoleModifiers.Shift) != 0, alt: true, (parsed.Modifiers & ConsoleModifiers.Control) != 0);
            }

            startIndex--;
        }
        else if (span.Length >= MinimalSequenceLength && TryParseTerminalInputSequence(buffer, terminalFormatStrings, out ConsoleKeyInfo parsed, ref startIndex, endIndex))
        {
            return parsed;
        }

        if (span.Length == 2 && span[0] == Escape && span[1] != Escape)
        {
            startIndex++; // skip the Escape
            keyInfo = ParseFromSingleChar(span[startIndex++], isAlt: true);
        }

        keyInfo = ParseFromSingleChar(span[startIndex++], isAlt: false);
        return true;*/
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
