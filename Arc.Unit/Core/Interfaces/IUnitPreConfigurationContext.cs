// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface IUnitPreConfigurationContext : IUnitSharedConfigurationContext
{
    // bool IsFirstBuilderRun { get; }

    void SetOptions<TOptions>(TOptions options)
        where TOptions : class;

    TContext GetCustomContext<TContext>()
        where TContext : IUnitCustomContext, new();
}
