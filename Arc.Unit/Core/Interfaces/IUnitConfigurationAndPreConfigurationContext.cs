// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Contextual information used by Setup delegate and provided to <see cref="UnitBuilder"/>.
/// </summary>
public interface IUnitConfigurationAndPreConfigurationContext
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
