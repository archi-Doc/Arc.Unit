// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Defines a contract for customizing the configuration context of a unit during its setup phase.
/// Implementations can configure the <see cref="IUnitConfigurationContext"/> as needed.
/// </summary>
public interface IUnitCustomContext
{
    /// <summary>
    /// Configures the provided <see cref="IUnitConfigurationContext"/> after the configuration delegates of all the builders have run.
    /// </summary>
    /// <param name="context">The configuration context.</param>
    void Configure(IUnitConfigurationContext context);
}
