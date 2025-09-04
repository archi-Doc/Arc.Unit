// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Provides contextual information and configuration methods used during the configuration phase of a unit.
/// </summary>
public interface IUnitConfigurationContext : IUnitPreConfigurationContext, IUnitConfigurationAndPostConfigurationContext
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> used for dependency injection and service registration.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds a logger resolver delegate that determines the appropriate <see cref="ILogOutput"/> and <see cref="ILogFilter"/>
    /// based on the log source and <see cref="LogLevel"/>.
    /// </summary>
    /// <param name="resolver">The <see cref="LoggerResolverDelegate"/> to add.</param>
    void AddLoggerResolver(LoggerResolverDelegate resolver);

    /// <summary>
    /// Clears all registered logger resolvers from the context.
    /// </summary>
    void ClearLoggerResolver();

    /// <summary>
    /// Adds a command type to the configuration context.
    /// </summary>
    /// <param name="commandType">The <see cref="Type"/> of the command to add.</param>
    /// <returns><see langword="true"/> if the command was successfully added; otherwise, <see langword="false"/>.</returns>
    bool AddCommand(Type commandType);

    /// <summary>
    /// Adds a subcommand type to the configuration context.
    /// </summary>
    /// <param name="commandType">The <see cref="Type"/> of the subcommand to add.</param>
    /// <returns><see langword="true"/> if the subcommand was successfully added; otherwise, <see langword="false"/>.</returns>
    bool AddSubcommand(Type commandType);

    /// <summary>
    /// Registers the specified type for instance creation.<br/>
    /// Instances are created by calling <see cref="UnitContext.CreateInstances()"/>.
    /// </summary>
    /// <typeparam name="T">The type to be instantiated and registered for creation.</typeparam>
    void RegisterInstanceCreation<T>();
}
