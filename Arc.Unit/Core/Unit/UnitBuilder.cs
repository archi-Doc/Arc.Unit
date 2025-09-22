// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Builder class of unit, for customizing dependencies.<br/>
/// <b>Unit = Builder + Product(Instance) + Function</b>
/// </summary>
/// <typeparam name="TProduct">The type of product.</typeparam>
public class UnitBuilder<TProduct> : UnitBuilder
    where TProduct : UnitProduct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBuilder{TUnit}"/> class.
    /// </summary>
    public UnitBuilder()
    {
    }

    /// <inheritdoc/>
    public override TProduct Build(string? args = null) => this.Build<TProduct>(args);

    /// <inheritdoc/>
    public override TProduct Build(string[] args) => this.Build<TProduct>(args);

    /// <inheritdoc/>
    public override UnitBuilder<TProduct> AddBuilder(UnitBuilder unitBuilder)
        => (UnitBuilder<TProduct>)base.AddBuilder(unitBuilder);

    /// <inheritdoc/>
    public override UnitBuilder<TProduct> PreConfigure(Action<IUnitPreConfigurationContext> @delegate)
        => (UnitBuilder<TProduct>)base.PreConfigure(@delegate);

    /// <inheritdoc/>
    public override UnitBuilder<TProduct> Configure(Action<IUnitConfigurationContext> configureDelegate)
        => (UnitBuilder<TProduct>)base.Configure(configureDelegate);

    /// <inheritdoc/>
    public override UnitBuilder<TProduct> PostConfigure(Action<IUnitPostConfigurationContext> configureDelegate)
        => (UnitBuilder<TProduct>)base.PostConfigure(configureDelegate);

    /// <inheritdoc/>
    public override TProduct GetBuiltUnit() => (TProduct)base.GetBuiltUnit();
}

/// <summary>
/// Builder class of unit, for customizing behaviors.<br/>
/// Unit is an independent unit of function and dependency.<br/>
/// </summary>
public class UnitBuilder
{
    #region FieldAndProperty

    private UnitProduct? builtUnit;
    private List<Action<IUnitPreConfigurationContext>> preConfigureActions = new();
    private List<Action<IUnitConfigurationContext>> configureActions = new();
    private List<Action<IUnitPostConfigurationContext>> postConfigureActions = new();
    private List<UnitBuilder> unitBuilders = new();

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBuilder"/> class.
    /// </summary>
    public UnitBuilder()
    {
    }

    protected Action<IUnitConfigurationContext>? CustomConfiguration { get; set; }

    /// <summary>
    /// Runs the given actions and build a unit.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see cref="UnitProduct"/>.</returns>
    public virtual UnitProduct Build(string[] args) => this.Build<UnitProduct>(args);

    /// <summary>
    /// Runs the given actions and build a unit.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see cref="UnitProduct"/>.</returns>
    public virtual UnitProduct Build(string? args = null) => this.Build<UnitProduct>(args);

    /// <summary>
    /// Adds a <see cref="UnitBuilder"/> instance to the builder.<br/>
    /// This can be called multiple times and the results will be additive.
    /// </summary>
    /// <param name="unitBuilder"><see cref="UnitBuilder"/>.</param>
    /// <returns>The same instance of the <see cref="UnitBuilder"/> for chaining.</returns>
    public virtual UnitBuilder AddBuilder(UnitBuilder unitBuilder)
    {
        this.unitBuilders.Add(unitBuilder);
        return this;
    }

    /// <summary>
    /// <b>Pre-configuration: Handles tasks such as setting options from the command line and handling load operations.</b><br/>
    /// Adds a delegate to the builder to pre-configure the unit.<br/>
    /// This method can be called multiple times, and all delegates will be combined.
    /// </summary>
    /// <param name="delegate">The delegate used to pre-configure the unit.</param>
    /// <returns>The same <see cref="UnitBuilder"/> instance for method chaining.</returns>
    public virtual UnitBuilder PreConfigure(Action<IUnitPreConfigurationContext> @delegate)
    {
        this.preConfigureActions.Add(@delegate);
        return this;
    }

    /// <summary>
    /// <b>Configuration: Handles registration with the DI container, adding commands, setting up the logger, and similar configuration tasks.</b><br/>
    /// Adds a delegate to the builder to configure the unit.<br/>
    /// This method can be called multiple times, and all delegates will be combined.
    /// </summary>
    /// <param name="delegate">The delegate used to configure the unit.</param>
    /// <returns>The same <see cref="UnitBuilder"/> instance for method chaining.</returns>
    public virtual UnitBuilder Configure(Action<IUnitConfigurationContext> @delegate)
    {
        this.configureActions.Add(@delegate);
        return this;
    }

    /// <summary>
    /// <b>Post-configuration: Executed after creating the instance (ServiceProvider), performing follow-up operations such as option settings.</b><br/>
    /// Adds a delegate to the builder to post-configure the unit.<br/>
    /// This method can be called multiple times, and all delegates will be combined.
    /// </summary>
    /// <param name="delegate">The delegate used to post-configure the unit.</param>
    /// <returns>The same <see cref="UnitBuilder"/> instance for method chaining.</returns>
    public virtual UnitBuilder PostConfigure(Action<IUnitPostConfigurationContext> @delegate)
    {
        this.postConfigureActions.Add(@delegate);
        return this;
    }

    /// <summary>
    /// Adds a delegate to the builder for setting up the option.<br/>
    /// This can be called multiple times and the results will be additive.
    /// </summary>
    /// <typeparam name="TOptions">The type of options class.</typeparam>
    /// <param name="delegate">The delegate for setting up the unit.</param>
    /// <returns>The same instance of the <see cref="UnitBuilder"/> for chaining.</returns>
    public virtual UnitBuilder PrepareOptions<TOptions>(Action<IUnitPostConfigurationContext, TOptions> @delegate)
        where TOptions : class
    {
        var ac = new Action<IUnitPostConfigurationContext, object>((context, options) => @delegate(context, (TOptions)options));
        return this;
    }

    public virtual UnitProduct GetBuiltUnit()
    {
        if (this.builtUnit == null)
        {
            throw new InvalidOperationException();
        }

        return this.builtUnit;
    }

    internal virtual TUnit Build<TUnit>(string[] args)
        where TUnit : UnitProduct
    {
        var s = args == null ? null : string.Join(' ', args);
        return this.Build<TUnit>(s);
    }

    internal virtual TUnit Build<TUnit>(string? args)
        where TUnit : UnitProduct
    {
        if (this.builtUnit != null)
        {
            throw new InvalidOperationException();
        }

        // Builder context
        var builderContext = new UnitBuilderContext(args);

        // Pre-configuration
        builderContext.ProcessedBuilderTypes.Clear();
        this.PreConfigureInternal(builderContext);

        // Configuration: UnitOptions, UnitLogger
        UnitOptions.Configure(builderContext);
        UnitLogger.Configure(builderContext);

        // Configuration
        builderContext.ProcessedBuilderTypes.Clear();
        this.ConfigureInternal(builderContext);

        // Custom configuration
        foreach (var x in builderContext.CustomContexts.Values)
        {
            if (x is IUnitCustomContext context)
            {
                context.ProcessContext(builderContext);
            }
        }

        // Register other services
        builderContext.TryAddSingleton<UnitCore>();
        builderContext.TryAddSingleton<UnitContext>();
        builderContext.TryAddSingleton<UnitOptions>();
        builderContext.TryAddSingleton<TUnit>();
        builderContext.TryAddSingleton<IConsoleService, ConsoleService>();
        builderContext.TryAddSingleton<RadioClass>();

        // Options instances
        foreach (var x in builderContext.OptionTypeToInstance)
        {
            builderContext.Services.Add(ServiceDescriptor.Singleton(x.Key, x.Value));
        }

        // Create a service provider
        var serviceProvider = builderContext.Services.BuildServiceProvider();
        builderContext.ServiceProvider = serviceProvider;

        // BuilderContext to UnitContext.
        var unitContext = serviceProvider.GetRequiredService<UnitContext>();
        unitContext.FromBuilderToUnitContext(serviceProvider, builderContext);

        // Post-configuration
        builderContext.ProcessedBuilderTypes.Clear();
        this.PostConfigureInternal(builderContext);

        var unit = serviceProvider.GetRequiredService<TUnit>();
        this.builtUnit = unit;
        return unit;
    }

    private void PreConfigureInternal(UnitBuilderContext context)
    {// Pre-configuration
        if (!context.ProcessedBuilderTypes.Add(this))
        {// Already processed.
            return;
        }

        // Unit builders
        foreach (var x in this.unitBuilders)
        {
            x.PreConfigureInternal(context);
        }

        // Actions
        foreach (var x in this.preConfigureActions)
        {
            x(context);
        }
    }

    private void ConfigureInternal(UnitBuilderContext context)
    {// Configuration
        if (!context.ProcessedBuilderTypes.Add(this))
        {// Already processed.
            return;
        }

        // Unit builders
        foreach (var x in this.unitBuilders)
        {
            x.ConfigureInternal(context);
            if (x.CustomConfiguration is { } customConfiguration)
            {
                customConfiguration(context);
            }
        }

        // Actions
        foreach (var x in this.configureActions)
        {
            x(context);
        }
    }

    private void PostConfigureInternal(UnitBuilderContext context)
    {// Post-configuration
        if (!context.ProcessedBuilderTypes.Add(this))
        {// Already processed.
            return;
        }

        // Unit builders
        foreach (var x in this.unitBuilders)
        {
            x.PostConfigureInternal(context);
        }

        // Actions
        foreach (var x in this.postConfigureActions)
        {
            x(context);
        }
    }
}
