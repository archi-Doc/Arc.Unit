// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Text;
using Arc.Threading;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Builder class of unit which creates the specified type of product.<br/>
/// <b>Unit = Builder + Product(Instance) + Function</b>
/// </summary>
/// <typeparam name="TProduct">The type of product created by <see cref="Build(string?)"/>.</typeparam>
public class UnitBuilder<TProduct> : UnitBuilder
    where TProduct : UnitProduct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBuilder{TProduct}"/> class.
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
    public override UnitBuilder<TProduct> Configure(Action<IUnitConfigurationContext> @delegate)
        => (UnitBuilder<TProduct>)base.Configure(@delegate);

    /// <inheritdoc/>
    public override UnitBuilder<TProduct> PostConfigure(Action<IUnitPostConfigurationContext> @delegate)
        => (UnitBuilder<TProduct>)base.PostConfigure(@delegate);

    /// <inheritdoc/>
    public override TProduct GetBuiltProduct() => (TProduct)base.GetBuiltProduct();
}

/// <summary>
/// Builder class of unit, which registers dependencies and builds a <see cref="UnitProduct"/>.<br/>
/// Add configuration delegates with <see cref="PreConfigure(Action{IUnitPreConfigurationContext})"/>,
/// <see cref="Configure(Action{IUnitConfigurationContext})"/> and <see cref="PostConfigure(Action{IUnitPostConfigurationContext})"/>,
/// combine other builders with <see cref="AddBuilder(UnitBuilder)"/>, and call <see cref="Build(string?)"/> once.
/// </summary>
public class UnitBuilder
{
    #region FieldAndProperty

    private static readonly Func<IServiceCollection, IServiceProvider> DefaultServiceProviderFactory =
        static services => ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);

    private readonly List<Action<IUnitPreConfigurationContext>> preConfigureActions = new();
    private readonly List<Action<IUnitConfigurationContext>> configureActions = new();
    private readonly List<Action<IUnitPostConfigurationContext>> postConfigureActions = new();
    private readonly List<UnitBuilder> unitBuilders = new();
    private UnitProduct? builtUnit;
    private Func<IServiceCollection, IServiceProvider> serviceProviderFactory = DefaultServiceProviderFactory;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBuilder"/> class.
    /// </summary>
    public UnitBuilder()
    {
    }

    /// <summary>
    /// Gets or sets a configuration delegate of a derived builder, which is executed after the configuration delegates of this builder.
    /// </summary>
    protected Action<IUnitConfigurationContext>? CustomConfiguration { get; set; }

    /// <summary>
    /// Runs the registered delegates and builds a unit (can be called only once).
    /// </summary>
    /// <param name="args">Command-line arguments (an argument which contains whitespace is enclosed in quotation marks).</param>
    /// <returns><see cref="UnitProduct"/>.</returns>
    /// <exception cref="InvalidOperationException">The unit has already been built.</exception>
    public virtual UnitProduct Build(string[] args) => this.Build<UnitProduct>(args);

    /// <summary>
    /// Runs the registered delegates and builds a unit (can be called only once).
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns><see cref="UnitProduct"/>.</returns>
    /// <exception cref="InvalidOperationException">The unit has already been built.</exception>
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
    /// Gets the product which was created by <see cref="Build(string?)"/>.
    /// </summary>
    /// <returns><see cref="UnitProduct"/>.</returns>
    /// <exception cref="InvalidOperationException">The unit has not been built yet.</exception>
    public virtual UnitProduct GetBuiltProduct()
    {
        if (this.builtUnit == null)
        {
            throw new InvalidOperationException();
        }

        return this.builtUnit;
    }

    /// <summary>
    /// Sets the factory which creates an <see cref="IServiceProvider"/> from the <see cref="IServiceCollection"/><br/>
    /// (the default factory calls <see cref="ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(IServiceCollection)"/>).
    /// </summary>
    /// <param name="factory">The service provider factory.</param>
    public void SetServiceProviderFactory(Func<IServiceCollection, IServiceProvider> factory)
    {
        this.serviceProviderFactory = factory;
    }

    internal virtual TUnit Build<TUnit>(string[] args)
        where TUnit : UnitProduct
        => this.Build<TUnit>(JoinArguments(args));

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
        builderContext.ProcessedBuilders.Clear();
        this.PreConfigureInternal(builderContext);

        // Configuration: UnitLogger
        LogUnit.Configure(builderContext);

        // Configuration
        builderContext.ProcessedBuilders.Clear();
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
        builderContext.TryAddSingleton<ExecutionRoot>();
        builderContext.TryAddSingleton<UnitContext>();
        builderContext.TryAddSingleton<UnitOptions>();
        builderContext.TryAddSingleton<TUnit>();
        builderContext.TryAddSingleton<IConsoleService, ConsoleService>();
        builderContext.Services.AddCrossChannel(true); // builderContext.TryAddSingleton<RadioClass>();

        // Options instances
        foreach (var x in builderContext.OptionTypeToInstance)
        {
            builderContext.Services.Add(ServiceDescriptor.Singleton(x.Key, x.Value));
        }

        // Create a service provider
        var serviceProvider = this.serviceProviderFactory(builderContext.Services);
        builderContext.ServiceProvider = serviceProvider;

        // BuilderContext to UnitContext.
        var unitContext = serviceProvider.GetRequiredService<UnitContext>();
        unitContext.FromBuilderToUnitContext(serviceProvider, builderContext);

        // Post-configuration
        builderContext.ProcessedBuilders.Clear();
        this.PostConfigureInternal(builderContext);

        var unit = serviceProvider.GetRequiredService<TUnit>();
        this.builtUnit = unit;
        return unit;
    }

    /// <summary>
    /// Joins command-line arguments into a single string.<br/>
    /// An argument which contains whitespace is enclosed in quotation marks, so that it is not split during parsing.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>The joined arguments.</returns>
    private static string? JoinArguments(string[]? args)
    {
        if (args is null || args.Length == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var x in args)
        {
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            if (RequiresQuotation(x))
            {
                sb.Append('\"').Append(x).Append('\"');
            }
            else
            {
                sb.Append(x);
            }
        }

        return sb.ToString();

        static bool RequiresQuotation(string arg)
        {
            var containsWhitespace = false;
            foreach (var c in arg)
            {
                if (char.IsWhiteSpace(c))
                {
                    containsWhitespace = true;
                    break;
                }
            }

            if (!containsWhitespace)
            {// No whitespace: no need to be enclosed.
                return false;
            }

            if (arg.Length >= 2)
            {// Already enclosed: "A B" 'A B' {A B}
                var first = arg[0];
                var last = arg[arg.Length - 1];
                if ((first == '\"' && last == '\"') ||
                    (first == '\'' && last == '\'') ||
                    (first == '{' && last == '}'))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private void PreConfigureInternal(UnitBuilderContext context)
    {// Pre-configuration
        if (!context.ProcessedBuilders.Add(this))
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
        if (!context.ProcessedBuilders.Add(this))
        {// Already processed.
            return;
        }

        // Unit builders
        foreach (var x in this.unitBuilders)
        {
            x.ConfigureInternal(context);
        }

        // Actions
        foreach (var x in this.configureActions)
        {
            x(context);
        }

        // Custom configuration
        this.CustomConfiguration?.Invoke(context);
    }

    private void PostConfigureInternal(UnitBuilderContext context)
    {// Post-configuration
        if (!context.ProcessedBuilders.Add(this))
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
