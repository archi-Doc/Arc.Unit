// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

/// <summary>
/// Concrete type which is registered in the DI container for the open generic type <see cref="FileLogger{TOption}"/>.
/// </summary>
/// <typeparam name="TOption">The type of options which determines the file path and the behavior.</typeparam>
internal sealed class FileLoggerFactory<TOption> : FileLogger<TOption>
    where TOption : FileLoggerOptions
{
    public FileLoggerFactory(ExecutionRoot root, LogUnit logUnit, TOption options)
        : base(root, logUnit, options)
    {
    }
}
