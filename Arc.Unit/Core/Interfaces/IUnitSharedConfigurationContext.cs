// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Arc.Unit;

public interface IUnitSharedConfigurationContext
{
    string UnitName { get; set; }

    string ProgramDirectory { get; set; }

    string DataDirectory { get; set; }

    UnitArguments Arguments { get; }

    TOptions GetOptions<TOptions>()
        where TOptions : class, new();

    void SetOptions<TOptions>(TOptions options)
        where TOptions : class, new();

    /*bool TryGetOptions<TOptions>([MaybeNullWhen(false)] out TOptions options)
        where TOptions : class;

    TOptions GetOrCreateOptions<TOptions>()
        where TOptions : class, new();*/
}
