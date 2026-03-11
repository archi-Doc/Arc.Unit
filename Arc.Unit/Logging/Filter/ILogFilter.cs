// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogFilter
{
    internal delegate LogWriter? FilterDelegate(LogFilterParameter param);

    public LogWriter? Filter(LogFilterParameter param);
}
