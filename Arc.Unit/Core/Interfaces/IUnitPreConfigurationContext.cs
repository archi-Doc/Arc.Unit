// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Provides contextual information and configuration methods used during the pre-configuration phase of a unit.
/// </summary>
public interface IUnitPreConfigurationContext : IUnitSharedConfigurationContext
{
    // bool IsFirstBuilderRun { get; }

    /// <summary>
    /// Sets the specified options for the unit.
    /// </summary>
    /// <typeparam name="TOptions">The type of the options to set.</typeparam>
    /// <param name="options">The options instance to set.</param>
    void SetOptions<TOptions>(TOptions options)
        where TOptions : class;

    /// <summary>
    /// Gets a custom context of the specified type, creating a new instance if necessary.
    /// </summary>
    /// <typeparam name="TContext">The type of the custom context to retrieve.</typeparam>
    /// <returns>An instance of <typeparamref name="TContext"/>.</returns>
    TContext GetCustomContext<TContext>()
        where TContext : IUnitCustomContext, new();
}
