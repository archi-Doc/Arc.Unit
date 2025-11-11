// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace ConsoleBufferTest;

#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter

internal static partial class Interop
{
    private static IntPtr InputHandle => Interop.Sys.GetStdHandle(-10);

    internal enum ControlCharacterNames : int
    {
        VINTR = 0,
        VQUIT = 1,
        VERASE = 2,
        VKILL = 3,
        VEOF = 4,
        VTIME = 5,
        VMIN = 6,
        VSWTC = 7,
        VSTART = 8,
        VSTOP = 9,
        VSUSP = 10,
        VEOL = 11,
        VREPRINT = 12,
        VDISCARD = 13,
        VWERASE = 14,
        VLNEXT = 15,
        VEOL2 = 16,
    }

    public enum BOOL : int
    {
        FALSE = 0,
        TRUE = 1,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct KEY_EVENT_RECORD
    {
        internal BOOL bKeyDown;
        internal ushort wRepeatCount;
        internal ushort wVirtualKeyCode;
        internal ushort wVirtualScanCode;
        internal ushort _uChar; // Union between WCHAR and ASCII char
        internal uint dwControlKeyState;

        // _uChar is stored as short to avoid any ambiguity for interop marshaling
        internal char uChar => (char)this._uChar;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct INPUT_RECORD
    {
        internal ushort EventType;
        internal KEY_EVENT_RECORD keyEvent;
    }

    internal static partial class Sys
    {
        private const string SystemNative = "libSystem.Native";

        [LibraryImport(SystemNative, EntryPoint = "SystemNative_ReadStdin", SetLastError = true)]
        internal static unsafe partial int ReadStdin(byte* buffer, int bufferSize);


        [LibraryImport(SystemNative, EntryPoint = "SystemNative_ReadStdin", SetLastError = true)]
        internal static unsafe partial int ReadStdin(Span<byte> buffer, int bufferSize);

        [LibraryImport(SystemNative, EntryPoint = "SystemNative_InitializeConsoleBeforeRead")]
        internal static partial void InitializeConsoleBeforeRead(byte minChars = 1, byte decisecondsTimeout = 0);

        [LibraryImport(SystemNative, EntryPoint = "SystemNative_UninitializeConsoleAfterRead")]
        internal static partial void UninitializeConsoleAfterRead();

        [LibraryImport(SystemNative, EntryPoint = "SystemNative_GetControlCharacters")]
        internal static unsafe partial void GetControlCharacters(Span<Interop.ControlCharacterNames> controlCharacterNames, Span<byte> controlCharacterValues, int controlCharacterLength, out byte posixDisableValue);

        [LibraryImport("kernel32.dll", EntryPoint = "ReadConsoleInputW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ReadConsoleInput(IntPtr hConsoleInput, out INPUT_RECORD buffer, int numInputRecords_UseOne, out int numEventsRead);

        [LibraryImport("kernel32.dll")]
        [SuppressGCTransition]
        internal static partial IntPtr GetStdHandle(int nStdHandle);  // param is NOT a handle, but it returns one!
    }
}
