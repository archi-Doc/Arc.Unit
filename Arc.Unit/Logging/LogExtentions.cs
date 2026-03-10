// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public static class LogExtentions
{
    public static void Log(this ILogWriter logger, string message)
        => logger.Log(0, message);
}
