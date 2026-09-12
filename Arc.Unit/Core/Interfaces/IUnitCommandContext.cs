// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Arc.Unit;

/// <summary>
/// Provides contextual information and configuration methods used during the configuration and post-configuration phase of a unit.
/// </summary>
public interface IUnitConfigurationAndPostConfigurationContext
{
    /// <summary>
    /// Gets the child group of a command. Register commands during Configure, before the provider is built.
    /// </summary>
    /// <param name="type">The command type.</param>
    /// <returns><see cref="CommandGroup"/>.</returns>
    CommandGroup GetCommandGroup([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type);

    /// <summary>
    /// Gets the top-level command group.
    /// </summary>
    /// <returns><see cref="CommandGroup"/>.</returns>
    CommandGroup GetCommandGroup();

    /// <summary>
    /// Gets the separate subcommand group.
    /// </summary>
    /// <returns><see cref="CommandGroup"/>.</returns>
    CommandGroup GetSubcommandGroup();
}
