// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Manages basic unit options.
/// </summary>
public record class UnitOptions
{
    public UnitOptions()
    {
    }

    internal void CopyFrom(UnitBuilderContext builderContext)
    {
        this.UnitName = builderContext.UnitName;
        this.ProgramDirectory = builderContext.ProgramDirectory;
        this.DataDirectory = builderContext.DataDirectory;
    }

    /// <summary>
    /// Gets a unit name.
    /// </summary>
    public string UnitName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a program directory.
    /// </summary>
    public string ProgramDirectory { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a data directory.
    /// </summary>
    public string DataDirectory { get; private set; } = string.Empty;
}
