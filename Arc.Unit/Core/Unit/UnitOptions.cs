// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Manages basic unit options.
/// </summary>
public record class UnitOptions
{
    public static void Configure(IUnitConfigurationContext context)
    {
        context.SetOptions(context.GetOptions<UnitOptions>() with
        {
            UnitName = context.UnitName,
            ProgramDirectory = context.ProgramDirectory,
            DataDirectory = context.DataDirectory,
        });
    }

    public UnitOptions()
    {
    }

    /// <summary>
    /// Gets a unit name.
    /// </summary>
    public string UnitName { get; init; } = string.Empty;

    /// <summary>
    /// Gets a program directory.
    /// </summary>
    public string ProgramDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Gets a data directory.
    /// </summary>
    public string DataDirectory { get; init; } = string.Empty;
}
