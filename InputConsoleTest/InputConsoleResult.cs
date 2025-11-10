// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.InputConsole;

public enum InputConsoleResult
{
    /// <summary>
    /// The input was completed successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The input was canceled by the user (e.g., pressing Esc).
    /// </summary>
    Canceled = 1,

    /// <summary>
    /// The application received a termination request (e.g., Ctrl+C).
    /// </summary>
    Terminated = 2,
}
