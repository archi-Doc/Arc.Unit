// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Arc.Unit;

/// <summary>
/// Contextual information used by Preload delegate and provided to <see cref="UnitBuilder"/>.
/// </summary>
public interface IUnitPreloadSetupContext
{
    string UnitName { get; set; }

    string ProgramDirectory { get; set; }

    string DataDirectory { get; set; }

    UnitArguments Arguments { get; }

    void GetOptions<TOptions>(out TOptions options)
        where TOptions : class;

    bool TryGetOptions<TOptions>([MaybeNullWhen(false)] out TOptions options)
        where TOptions : class;

    TOptions GetOrCreateOptions<TOptions>()
        where TOptions : class, new();
}
