// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Arc.Threading;

namespace Arc.Unit;

public class ConsoleLogger : BufferedLogOutput
{
#pragma warning disable SA1310 // Field names should not contain underscore
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
#pragma warning restore SA1310 // Field names should not contain underscore

    private readonly ConsoleLoggerOptions options;
    private readonly ConsoleLoggerWorker? worker;

    public ConsoleLogger(ExecutionRoot root, LogUnit unitLogger, ConsoleLoggerOptions options)
        : base(unitLogger)
    {
        // Console
        EnableVirtualTerminalProcessing();

        this.Formatter = new(options.FormatterOptions);
        if (options.EnableBuffering)
        {
            this.worker = new(root, this);
        }

        this.options = options;
    }

    public override void Output(LogEvent param)
    {
        var worker = this.worker;
        if (worker is null)
        {
            // Console output might cause unexpected exceptions after the console window is closed (IConsoleService handles them).
            this.Formatter.FormatAndWriteLine(param.LogService.ConsoleService, param);
            return;
        }

        if (this.options.MaxQueue <= 0 || worker.Count < this.options.MaxQueue)
        {
            worker.Add(param);
        }
    }

    public override Task<int> Flush(bool terminate) => this.worker?.Flush(terminate) ?? Task.FromResult(0);

    internal SimpleLogFormatter Formatter { get; init; }

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    /// <summary>
    /// Enables the escape sequences of the Windows console (other platforms support them by default).
    /// </summary>
    private static void EnableVirtualTerminalProcessing()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            if (GetConsoleMode(iStdOut, out uint outConsoleMode))
            {
                outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
                SetConsoleMode(iStdOut, outConsoleMode);
            }
        }
        catch
        {
        }
    }
}
