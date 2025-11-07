// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace ConsoleBufferTest;

internal static partial class Interop
{
    private static IntPtr InputHandle => Interop.Sys.GetStdHandle(-10);

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
        private ushort _uChar; // Union between WCHAR and ASCII char
        internal uint dwControlKeyState;

        // _uChar is stored as short to avoid any ambiguity for interop marshaling
        internal char uChar => (char)_uChar;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct INPUT_RECORD
    {
        internal ushort EventType;
        internal KEY_EVENT_RECORD keyEvent;
    }

    internal static partial class Sys
    {
        [LibraryImport("libSystem.Native", EntryPoint = "SystemNative_ReadStdin", SetLastError = true)]
        internal static unsafe partial int ReadStdin(byte* buffer, int bufferSize);

        [LibraryImport("libSystem.Native", EntryPoint = "SystemNative_InitializeConsoleBeforeRead")]
        internal static partial void InitializeConsoleBeforeRead(byte minChars = 1, byte decisecondsTimeout = 0);

        [LibraryImport("libSystem.Native", EntryPoint = "SystemNative_UninitializeConsoleAfterRead")]
        internal static partial void UninitializeConsoleAfterRead();

        [LibraryImport("kernel32.dll", EntryPoint = "ReadConsoleInputW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ReadConsoleInput(IntPtr hConsoleInput, out INPUT_RECORD buffer, int numInputRecords_UseOne, out int numEventsRead);

        [LibraryImport("kernel32.dll")]
        [SuppressGCTransition]
        internal static partial IntPtr GetStdHandle(int nStdHandle);  // param is NOT a handle, but it returns one!
    }
}
