// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

internal sealed class FileLoggerFactory<TOption> : FileLogger<TOption>
    where TOption : FileLoggerOptions
{
    public FileLoggerFactory(ExecutionRoot root, LogUnit unitLogger, TOption options)
        : base(root, unitLogger, options)
    {
    }
}
