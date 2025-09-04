// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Provides contextual information and configuration methods used during the configuration and post-configuration phase of a unit.
/// </summary>
public interface IUnitConfigurationAndPostConfigurationContext
{
    /// <summary>
    /// Gets <see cref="CommandGroup"/> of the specified command type.
    /// </summary>
    /// <param name="type">The command type.</param>
    /// <returns><see cref="CommandGroup"/>.</returns>
    CommandGroup GetCommandGroup(Type type);

    /// <summary>
    /// Gets <see cref="CommandGroup"/> of command.
    /// </summary>
    /// <returns><see cref="CommandGroup"/>.</returns>
    CommandGroup GetCommandGroup();

    /// <summary>
    /// Gets <see cref="CommandGroup"/> of subcommand.
    /// </summary>
    /// <returns><see cref="CommandGroup"/>.</returns>
    CommandGroup GetSubcommandGroup();
}
