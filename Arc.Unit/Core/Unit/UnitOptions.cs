// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Holds the basic unit information determined during the build process (registered as a singleton service).
/// </summary>
public record class UnitOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOptions"/> class.
    /// </summary>
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
    /// Gets the unit name.
    /// </summary>
    public string UnitName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the directory path where the program is located.
    /// </summary>
    public string ProgramDirectory { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the directory path used for data storage (empty if not specified).
    /// </summary>
    public string DataDirectory { get; private set; } = string.Empty;
}
