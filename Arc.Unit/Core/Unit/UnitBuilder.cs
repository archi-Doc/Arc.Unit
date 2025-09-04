// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Builder class of unit, for customizing dependencies.<br/>
/// Unit is an independent unit of function and dependency.<br/>
/// </summary>
/// <typeparam name="TUnit">The type of unit.</typeparam>
public class UnitBuilder<TUnit> : UnitBuilder
    where TUnit : BuiltUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBuilder{TUnit}"/> class.
    /// </summary>
    public UnitBuilder()
    {
    }

    /// <inheritdoc/>
    public override TUnit Build(string? args = null) => this.Build<TUnit>(args);

    /// <inheritdoc/>
    public override TUnit Build(string[] args) => this.Build<TUnit>(args);

    /// <inheritdoc/>
    public override UnitBuilder<TUnit> AddBuilder(UnitBuilder unitBuilder)
        => (UnitBuilder<TUnit>)base.AddBuilder(unitBuilder);

    /// <inheritdoc/>
    public override UnitBuilder<TUnit> PreConfigure(Action<IUnitPreConfigurationContext> @delegate)
        => (UnitBuilder<TUnit>)base.PreConfigure(@delegate);

    /// <inheritdoc/>
    public override UnitBuilder<TUnit> Configure(Action<IUnitConfigurationContext> configureDelegate)
        => (UnitBuilder<TUnit>)base.Configure(configureDelegate);

    /// <inheritdoc/>
    public override UnitBuilder<TUnit> SetupOptions<TOptions>(Action<IUnitPostConfigurationContext, TOptions> @delegate)
        where TOptions : class
        => (UnitBuilder<TUnit>)base.SetupOptions(@delegate);

    /// <inheritdoc/>
    public override TUnit GetBuiltUnit() => (TUnit)base.GetBuiltUnit();
}

/// <summary>
/// Builder class of unit, for customizing behaviors.<br/>
/// Unit is an independent unit of function and dependency.<br/>
/// </summary>
public class UnitBuilder
{
    #region FieldAndProperty

    private BuiltUnit? builtUnit;
    private List<Action<IUnitPreConfigurationContext>> preConfigureActions = new();
    private List<Action<IUnitConfigurationContext>> configureActions = new();
    private List<Action<IUnitPostConfigurationContext>> postConfigureActions = new();
    private List<SetupItem> setupItems = new();
    private List<UnitBuilder> unitBuilders = new();

    #endregion

    private record SetupItem(Type Type, Action<IUnitPostConfigurationContext, object> Action);

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
    /// <returns><see cref="BuiltUnit"/>.</returns>
    public virtual BuiltUnit Build(string[] args) => this.Build<BuiltUnit>(args);

    /// <summary>
    /// Runs the given actions and build a unit.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see cref="BuiltUnit"/>.</returns>
    public virtual BuiltUnit Build(string? args = null) => this.Build<BuiltUnit>(args);

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
    public virtual UnitBuilder SetupOptions<TOptions>(Action<IUnitPostConfigurationContext, TOptions> @delegate)
        where TOptions : class
    {
        var ac = new Action<IUnitPostConfigurationContext, object>((context, options) => @delegate(context, (TOptions)options));
        var item = new SetupItem(typeof(TOptions), ac);
        this.setupItems.Add(item);
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
        var item = new SetupItem(typeof(TOptions), ac);
        this.setupItems.Add(item);
        return this;
    }

    public virtual BuiltUnit GetBuiltUnit()
    {
        if (this.builtUnit == null)
        {
            throw new InvalidOperationException();
        }

        return this.builtUnit;
    }

    internal virtual TUnit Build<TUnit>(string[] args)
        where TUnit : BuiltUnit
    {
        var s = args == null ? null : string.Join(' ', args);
        return this.Build<TUnit>(s);
    }

    internal virtual TUnit Build<TUnit>(string? args)
        where TUnit : BuiltUnit
    {
        if (this.builtUnit != null)
        {
            throw new InvalidOperationException();
        }

        // Builder context.
        var builderContext = new UnitBuilderContext();

        // Pre-Configuration
        builderContext.ProcessedBuilderTypes.Clear();
        this.PreConfigureInternal(builderContext, args);

        // Configure
        UnitOptions.Configure(builderContext); // Unit options
        UnitLogger.Configure(builderContext); // Logger
        builderContext.ProcessedBuilderTypes.Clear();
        this.ConfigureInternal(builderContext);

        // Custom
        foreach (var x in builderContext.CustomContexts.Values)
        {
            if (x is IUnitCustomContext context)
            {
                context.Configure(builderContext);
            }
        }

        builderContext.TryAddSingleton<UnitCore>();
        builderContext.TryAddSingleton<UnitContext>();
        builderContext.TryAddSingleton<UnitOptions>();
        builderContext.TryAddSingleton<TUnit>();
        builderContext.TryAddSingleton<IConsoleService, ConsoleService>();
        builderContext.TryAddSingleton<RadioClass>(); // Unit radio

        // Setup classes
        foreach (var x in this.setupItems)
        {
            builderContext.TryAddSingleton(x.Type);
        }

        // Options instances
        foreach (var x in builderContext.OptionTypeToInstance)
        {
            builderContext.Services.Add(ServiceDescriptor.Singleton(x.Key, x.Value));
        }

        var serviceProvider = builderContext.Services.BuildServiceProvider();
        builderContext.ServiceProvider = serviceProvider;

        // BuilderContext to UnitContext.
        var unitContext = serviceProvider.GetRequiredService<UnitContext>();
        unitContext.FromBuilderToUnit(serviceProvider, builderContext);

        // Setup
        this.SetupInternal(builderContext);

        var unit = serviceProvider.GetRequiredService<TUnit>();
        this.builtUnit = unit;
        return unit;
    }

    private void PreConfigureInternal(UnitBuilderContext context, string? args)
    {// Pre-configuration
        if (!context.ProcessedBuilderTypes.Add(this.GetType()))
        {// Already processed.
            return;
        }

        // Arguments
        if (args != null)
        {
            context.Arguments.Add(args);
        }

        // Directory
        context.SetDirectory();

        // Unit builders
        foreach (var x in this.unitBuilders)
        {
            x.PreConfigureInternal(context, args);
        }

        // Actions
        foreach (var x in this.preConfigureActions)
        {
            x(context);
        }
    }

    private void ConfigureInternal(UnitBuilderContext context)
    {// Configuration
        if (!context.ProcessedBuilderTypes.Add(this.GetType()))
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

        // Configure actions
        foreach (var x in this.configureActions)
        {
            x(context);
        }
    }

    private void SetupInternal(UnitBuilderContext builderContext)
    {
        // Unit builders
        foreach (var x in this.unitBuilders)
        {
            x.SetupInternal(builderContext);
        }

        // Actions
        foreach (var x in this.setupItems)
        {
            var instance = builderContext.ServiceProvider!.GetRequiredService(x.Type);
            x.Action(builderContext, instance);
        }
    }
}
