// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Arc.Unit;

/// <summary>
/// Provides access to the command groups during the configuration and post-configuration phases of a unit.
/// </summary>
public interface IUnitCommandContext
{
    /// <summary>
    /// Gets the command group identified by the specified type (e.g. the child group of a command). Register commands during Configure, before the provider is built.<br/>
    /// The group type is not registered in the DI container; add it with <see cref="IUnitConfigurationContext.AddCommand(Type, Microsoft.Extensions.DependencyInjection.ServiceLifetime)"/> if it is a command.
    /// </summary>
    /// <param name="groupType">The type which identifies the group (usually the parent command type).</param>
    /// <returns><see cref="CommandGroup"/>.</returns>
    CommandGroup GetCommandGroup([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type groupType);

    /// <summary>
    /// Gets the top-level command group.
    /// </summary>
    /// <returns><see cref="CommandGroup"/>.</returns>
    CommandGroup GetTopLevelCommandGroup();

    /// <summary>
    /// Gets the separate subcommand group.
    /// </summary>
    /// <returns><see cref="CommandGroup"/>.</returns>
    CommandGroup GetSubcommandGroup();
}
