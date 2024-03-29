﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Arc.Unit;

/// <summary>
/// Contextual information used by Preload delegate and provided to <see cref="UnitBuilder"/>.
/// </summary>
public interface IUnitPreloadContext : IUnitPreloadSetupContext
{
    bool FirstBuilderRun { get; }

    void SetOptions<TOptions>(TOptions options)
        where TOptions : class;

    TContext GetCustomContext<TContext>()
        where TContext : IUnitCustomContext, new();
}
