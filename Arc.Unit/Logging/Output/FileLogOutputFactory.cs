// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

/// <summary>
/// Concrete type which is registered in the DI container for the open generic type <see cref="FileLogOutput{TOptions}"/>.
/// </summary>
/// <typeparam name="TOptions">The type of options which determines the file path and the behavior.</typeparam>
internal sealed class FileLogOutputFactory<TOptions> : FileLogOutput<TOptions>
    where TOptions : FileLogOutputOptions
{
    public FileLogOutputFactory(ExecutionRoot root, LogUnit logUnit, TOptions options)
        : base(root, logUnit, options)
    {
    }
}
