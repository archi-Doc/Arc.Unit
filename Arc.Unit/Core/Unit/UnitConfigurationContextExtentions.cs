// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc.Unit;

/// <summary>
/// Provides convenience extension methods for registering services into an <see cref="IUnitConfigurationContext"/>.
/// </summary>
public static class UnitConfigurationContextExtentions
{
    /// <summary>
    /// Registers a <see cref="UnitBase"/>-derived unit type as a singleton service and registers it for default instance creation.
    /// </summary>
    /// <typeparam name="TUnit">The unit type to register.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    /// <remarks>
    /// In addition to calling <see cref="ServiceCollectionServiceExtensions.AddSingleton{TService}(IServiceCollection)"/>,
    /// this method calls <see cref="IUnitConfigurationContext.RegisterDefaultInstantiableType{T}()"/> so the unit can be
    /// created when <c>UnitContext.CreateInstances()</c> is invoked.
    /// </remarks>
    public static void AddSingletonUnit<TUnit>(this IUnitConfigurationContext context)
        where TUnit : UnitBase
    {
        context.Services.AddSingleton<TUnit>();
        context.RegisterDefaultInstantiableType<TUnit>();
    }

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a singleton service.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    public static void AddSingleton<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.AddSingleton<TService>();

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a scoped service.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    public static void AddScoped<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.AddScoped<TService>();

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a transient service.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    public static void AddTransient<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.AddTransient<TService>();

    /// <summary>
    /// Registers <paramref name="serviceType"/> as a singleton service.
    /// </summary>
    /// <param name="context">The current unit configuration context.</param>
    /// <param name="serviceType">The service type to register.</param>
    public static void AddSingleton(this IUnitConfigurationContext context, Type serviceType) => context.Services.AddSingleton(serviceType);

    /// <summary>
    /// Registers <paramref name="serviceType"/> as a scoped service.
    /// </summary>
    /// <param name="context">The current unit configuration context.</param>
    /// <param name="serviceType">The service type to register.</param>
    public static void AddScoped(this IUnitConfigurationContext context, Type serviceType) => context.Services.AddScoped(serviceType);

    /// <summary>
    /// Registers <paramref name="serviceType"/> as a transient service.
    /// </summary>
    /// <param name="context">The current unit configuration context.</param>
    /// <param name="serviceType">The service type to register.</param>
    public static void AddTransient(this IUnitConfigurationContext context, Type serviceType) => context.Services.AddTransient(serviceType);

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a singleton service with implementation type <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    public static void AddSingleton<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.AddSingleton<TService, TImplementation>();

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a scoped service with implementation type <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    public static void AddScoped<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.AddScoped<TService, TImplementation>();

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a transient service with implementation type <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    public static void AddTransient<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.AddTransient<TService, TImplementation>();

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a singleton service if it has not already been registered.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    /// <remarks>
    /// This uses the <c>TryAdd*</c> semantics from <c>Microsoft.Extensions.DependencyInjection.Extensions</c>.
    /// </remarks>
    public static void TryAddSingleton<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.TryAddSingleton<TService>();

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a scoped service if it has not already been registered.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    /// <remarks>
    /// This uses the <c>TryAdd*</c> semantics from <c>Microsoft.Extensions.DependencyInjection.Extensions</c>.
    /// </remarks>
    public static void TryAddScoped<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.TryAddScoped<TService>();

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a transient service if it has not already been registered.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    /// <remarks>
    /// This uses the <c>TryAdd*</c> semantics from <c>Microsoft.Extensions.DependencyInjection.Extensions</c>.
    /// </remarks>
    public static void TryAddTransient<TService>(this IUnitConfigurationContext context)
        where TService : class => context.Services.TryAddTransient<TService>();

    /// <summary>
    /// Registers <paramref name="serviceType"/> as a singleton service if it has not already been registered.
    /// </summary>
    /// <param name="context">The current unit configuration context.</param>
    /// <param name="serviceType">The service type to register.</param>
    public static void TryAddSingleton(this IUnitConfigurationContext context, Type serviceType) => context.Services.TryAddSingleton(serviceType);

    /// <summary>
    /// Registers <paramref name="serviceType"/> as a scoped service if it has not already been registered.
    /// </summary>
    /// <param name="context">The current unit configuration context.</param>
    /// <param name="serviceType">The service type to register.</param>
    public static void TryAddScoped(this IUnitConfigurationContext context, Type serviceType) => context.Services.TryAddScoped(serviceType);

    /// <summary>
    /// Registers <paramref name="serviceType"/> as a transient service if it has not already been registered.
    /// </summary>
    /// <param name="context">The current unit configuration context.</param>
    /// <param name="serviceType">The service type to register.</param>
    public static void TryAddTransient(this IUnitConfigurationContext context, Type serviceType) => context.Services.TryAddTransient(serviceType);

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a singleton service with implementation type <typeparamref name="TImplementation"/>
    /// if it has not already been registered.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    public static void TryAddSingleton<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.TryAddSingleton<TService, TImplementation>();

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a scoped service with implementation type <typeparamref name="TImplementation"/>
    /// if it has not already been registered.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    public static void TryAddScoped<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.TryAddScoped<TService, TImplementation>();

    /// <summary>
    /// Registers <typeparamref name="TService"/> as a transient service with implementation type <typeparamref name="TImplementation"/>
    /// if it has not already been registered.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
    /// <param name="context">The current unit configuration context.</param>
    public static void TryAddTransient<TService, TImplementation>(this IUnitConfigurationContext context)
        where TService : class
        where TImplementation : class, TService => context.Services.TryAddTransient<TService, TImplementation>();
}
