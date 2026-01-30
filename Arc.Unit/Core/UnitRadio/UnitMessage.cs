// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

public static class UnitMessage
{// Create instance -> Prepare -> LoadAsync -> StartAsync -> Stop -> TerminateAsync, SaveAsync (after Prepare)
    public record Prepare();

    public record StartAsync(ThreadCoreBase ParentCore);

    public record Stop();

    public record TerminateAsync();

    public record LoadAsync(string DataPath);

    public record SaveAsync(string DataPath);
}
