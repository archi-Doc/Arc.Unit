// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogContext
{
    public ILog? TryGet<TLogOutput>(LogLevel logLevel = LogLevel.Information);
}
