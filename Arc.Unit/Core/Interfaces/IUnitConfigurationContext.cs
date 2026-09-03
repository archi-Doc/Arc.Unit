// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
    /// <param name="lifetime">The service lifetime for the command.</param>
    /// <returns><see langword="true"/> if the command was successfully added; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// All the members of <paramref name="commandType"/> are preserved when the application is trimmed,
    /// since the command type is usually processed by a command-line parser using reflection.
    /// </remarks>
    bool AddCommand([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, ServiceLifetime lifetime = ServiceLifetime.Scoped);

    /// <summary>
    /// Adds a subcommand type to the configuration context.
    /// </summary>
    /// <param name="commandType">The <see cref="Type"/> of the subcommand to add.</param>
    /// <param name="lifetime">The service lifetime for the command.</param>
    /// <returns><see langword="true"/> if the subcommand was successfully added; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// All the members of <paramref name="commandType"/> are preserved when the application is trimmed,
    /// since the command type is usually processed by a command-line parser using reflection.
    /// </remarks>
    bool AddSubcommand([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, ServiceLifetime lifetime = ServiceLifetime.Scoped);

    /// <summary>
    /// Registers the specified type so that an instance is created by <see cref="UnitContext.CreateInstances()"/>.<br/>
    /// The type must also be registered in <see cref="Services"/> (<see cref="UnitConfigurationContextExtensions.AddSingletonUnit{TUnit}(IUnitConfigurationContext)"/> does both).
    /// </summary>
    /// <typeparam name="T">The type to be instantiated.</typeparam>
    void RegisterInstanceCreation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>();
}
