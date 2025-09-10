// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Defines a contract for customizing the configuration context of a unit during its setup phase.
/// Implementations can process the <see cref="IUnitConfigurationContext"/> as needed.
/// </summary>
public interface IUnitCustomContext
{
    /// <summary>
    /// Processes the provided <see cref="IUnitConfigurationContext"/> during unit configuration.
    /// </summary>
    /// <param name="context">The configuration context to process.</param>
    void ProcessContext(IUnitConfigurationContext context);
}
