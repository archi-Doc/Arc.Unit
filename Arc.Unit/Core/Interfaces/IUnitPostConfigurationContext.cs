// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Contextual information used by Setup delegate and provided to <see cref="UnitBuilder"/>.
/// </summary>
public interface IUnitPostConfigurationContext : IUnitPreConfigurationContext, IUnitConfigurationAndPreConfigurationContext
{
    /// <summary>
    /// Gets <see cref="IServiceProvider"/>.
    /// </summary>
    IServiceProvider ServiceProvider { get; }
}
