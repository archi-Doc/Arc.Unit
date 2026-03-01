// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc.Unit;

/// <summary>
/// <see cref="CommandGroup"/> is a collection of command types.
/// </summary>
public class CommandGroup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandGroup"/> class.
    /// </summary>
    /// <param name="context"><see cref="UnitBuilderContext"/>.</param>
    public CommandGroup(IUnitConfigurationContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Adds a command type to the <see cref="CommandGroup"/>.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="lifetime">The service lifetime for the command.</param>
    /// <returns><see langword="true"/>: Successfully added.</returns>
    public bool AddCommand(Type commandType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (this.commandSet.Contains(commandType))
        {
            return false;
        }
        else
        {
            // this.context.TryAddSingleton(commandType);
            this.context.Services.TryAdd(ServiceDescriptor.Describe(commandType, commandType, lifetime));
            this.commandSet.Add(commandType);
            this.commandList.Add(commandType);
            return true;
        }
    }

    /// <summary>
    /// Gets an array of command types.
    /// </summary>
    /// <returns>An array of <see cref="Type"/>.</returns>
    public Type[] ToArray() => this.commandList.ToArray();

    private IUnitConfigurationContext context;
    private List<Type> commandList = new();
    private HashSet<Type> commandSet = new();
}
