// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Manages basic unit options.
/// </summary>
public class UnitOptions
{
    public static void Configure(IUnitConfigurationContext context)
    {
        if (!context.TryGetOptions<UnitOptions>(out var options))
        {
            options = new UnitOptions();
            options.UnitName = context.UnitName;
            options.RootDirectory = context.RootDirectory;
            options.DataDirectory = context.DataDirectory;
            context.SetOptions(options);
        }
    }

    public UnitOptions()
    {
    }

    /// <summary>
    /// Gets or sets a unit name.
    /// </summary>
    public string UnitName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a root directory.
    /// </summary>
    public string RootDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a data directory.
    /// </summary>
    public string DataDirectory { get; set; } = string.Empty;
}
