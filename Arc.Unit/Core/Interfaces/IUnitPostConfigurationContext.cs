// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Provides contextual information and configuration methods used during the post-configuration phase of a unit.
/// </summary>
public interface IUnitPostConfigurationContext : IUnitPreConfigurationContext, IUnitConfigurationAndPostConfigurationContext
{
    /// <summary>
    /// Gets <see cref="IServiceProvider"/>.
    /// </summary>
    IServiceProvider ServiceProvider { get; }
}
