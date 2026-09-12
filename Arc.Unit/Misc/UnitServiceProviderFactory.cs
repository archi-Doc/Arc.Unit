// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Creates an <see cref="IServiceProviderFactory{UnitBuilder}"/> instance from <see cref="UnitBuilder"/> instance.
/// </summary>
public class UnitServiceProviderFactory : IServiceProviderFactory<UnitBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitServiceProviderFactory"/> class.
    /// </summary>
    /// <param name="builder">The underlying <see cref="UnitBuilder"/> instance which is returned by <see cref="CreateBuilder(IServiceCollection)"/>.</param>
    public UnitServiceProviderFactory(UnitBuilder builder)
    {
        this.builder = builder;
    }

    /// <inheritdoc/>
    public UnitBuilder CreateBuilder(IServiceCollection services)
    {
        this.builder.Configure(context =>
        {
            foreach (var x in services)
            {
                context.Services.Add(x);
            }
        });

        return this.builder;
    }

    /// <inheritdoc/>
    public IServiceProvider CreateServiceProvider(UnitBuilder containerBuilder)
    {
        var unit = containerBuilder.Build();
        return unit.Context.ServiceProvider;
    }

    private readonly UnitBuilder builder;
}
