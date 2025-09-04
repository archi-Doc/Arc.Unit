// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Provides contextual information and configuration methods used during the pre-configuration phase of a unit.
/// </summary>
public interface IUnitPreConfigurationContext
{
    /// <summary>
    /// Gets or sets the name of the unit being configured.
    /// </summary>
    string UnitName { get; set; }

    /// <summary>
    /// Gets or sets the directory path where the program is located.
    /// </summary>
    string ProgramDirectory { get; set; }

    /// <summary>
    /// Gets or sets the directory path used for data storage.
    /// </summary>
    string DataDirectory { get; set; }

    /// <summary>
    /// Gets the parsed command-line arguments for the unit.
    /// </summary>
    UnitArguments Arguments { get; }

    /// <summary>
    /// Retrieves the options object of the specified type for the unit.
    /// </summary>
    /// <typeparam name="TOptions">The type of the options object to retrieve.</typeparam>
    /// <returns>An instance of <typeparamref name="TOptions"/> containing the current options.</returns>
    TOptions GetOptions<TOptions>()
        where TOptions : class, new();

    /// <summary>
    /// Sets the options object of the specified type for the unit.
    /// </summary>
    /// <typeparam name="TOptions">The type of the options object to set.</typeparam>
    /// <param name="options">The options object to assign.</param>
    void SetOptions<TOptions>(TOptions options)
        where TOptions : class, new();

    /// <summary>
    /// Gets a custom context of the specified type, creating a new instance if necessary.
    /// </summary>
    /// <typeparam name="TContext">The type of the custom context to retrieve.</typeparam>
    /// <returns>An instance of <typeparamref name="TContext"/>.</returns>
    TContext GetCustomContext<TContext>()
        where TContext : IUnitCustomContext, new();
}
