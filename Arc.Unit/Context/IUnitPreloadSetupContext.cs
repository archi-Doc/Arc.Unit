// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Arc.Unit;

/// <summary>
/// Contextual information used by Preload delegate and provided to <see cref="UnitBuilder"/>.
/// </summary>
public interface IUnitPreloadSetupContext
{
    public string UnitName { get; set; }

    public string RootDirectory { get; set; }

    public string DataDirectory { get; set; }

    public UnitArguments Arguments { get; }

    public void GetOptions<TOptions>(out TOptions options)
        where TOptions : class;

    public bool TryGetOptions<TOptions>([MaybeNullWhen(false)] out TOptions options)
        where TOptions : class;

    public TOptions GetOrCreateOptions<TOptions>()
        where TOptions : class, new();
}
