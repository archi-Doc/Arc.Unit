// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogFilter
{
    internal delegate ILogWriter? FilterDelegate(LogFilterParameter param);

    public ILogWriter? Filter(LogFilterParameter param);
}
