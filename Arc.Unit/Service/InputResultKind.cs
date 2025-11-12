// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Represents the result kind of an input operation, indicating whether the operation completed successfully, was canceled, or was terminated.
/// </summary>
public enum InputResultKind
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
