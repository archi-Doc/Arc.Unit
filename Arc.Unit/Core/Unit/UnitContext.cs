// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Contextual information shared by the units of a product (created by <see cref="UnitBuilder.Build(string?)"/>).<br/>
/// It provides the service provider, the execution root and the registered commands, and sends notifications to the units.<br/>
/// Since it exposes singleton data to every unit, prefer constructor injection of the required services where possible.
/// </summary>
public sealed class UnitContext
{
    #region FieldAndProperty

    /// <summary>
    /// Gets or sets a value indicating whether a termination has been requested (the units may set and check this flag).
    /// </summary>
    public bool TerminationRequested { get; set; }

    /// <summary>
    /// Gets an instance of <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; } = default!;

    /// <summary>
    /// Gets the <see cref="ExecutionRoot"/> associated with this context.
    /// </summary>
    public ExecutionRoot ExecutionRoot { get; private set; } = default!;

    /// <summary>
    /// Gets the <see cref="UnitOptions"/> associated with this context.
    /// </summary>
    public UnitOptions Options { get; private set; } = new();

    /// <summary>
    /// Gets the <see cref="RadioClass"/> which delivers the notifications (Prepare/Start/Stop/Terminate/Load/Save) to the units.
    /// </summary>
    public RadioClass Radio { get; private set; } = default!;

    /// <summary>
    /// Gets an array of <see cref="Type"/> registered by <see cref="IUnitConfigurationContext.RegisterInstanceCreation{T}()"/>.<br/>
    /// Note that instances are actually created by calling <see cref="UnitContext.CreateInstances()"/>.
    /// </summary>
    public Type[] InstanceCreationTypes { get; private set; } = [];

    /// <summary>
    /// Gets an array of command <see cref="Type"/> added by <see cref="IUnitConfigurationContext.AddCommand(Type, ServiceLifetime)"/>.
    /// </summary>
    public Type[] Commands => this.CommandDictionary[typeof(UnitBuilderContext.TopCommand)];

    /// <summary>
    /// Gets an array of subcommand <see cref="Type"/> added by <see cref="IUnitConfigurationContext.AddSubcommand(Type, ServiceLifetime)"/>.
    /// </summary>
    public Type[] Subcommands => this.CommandDictionary[typeof(UnitBuilderContext.SubCommand)];

    /// <summary>
    /// Gets a collection of command <see cref="Type"/> (keys) and subcommand <see cref="Type"/> (values).
    /// </summary>
    public Dictionary<Type, Type[]> CommandDictionary { get; private set; } = new();

    /// <summary>
    /// Gets an array of <see cref="LoggerResolverDelegate"/> registered by <see cref="IUnitConfigurationContext.AddLoggerResolver(LoggerResolverDelegate)"/>.
    /// </summary>
    public LoggerResolverDelegate[] LoggerResolvers { get; private set; } = [];

    private Dictionary<Type, object> optionTypeToInstance = new();

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitContext"/> class.
    /// </summary>
    public UnitContext()
    {
    }

    /// <summary>
    /// Retrieves an options instance of type <typeparamref name="TOptions"/> from the <see cref="ServiceProvider"/> or internal storage.
    /// </summary>
    /// <typeparam name="TOptions">
    /// The type of the options class to retrieve. Must be a reference type with a parameterless constructor.
    /// </typeparam>
    /// <returns>
    /// An instance of <typeparamref name="TOptions"/> if available; otherwise, <c>null</c>.
    /// </returns>
    public TOptions? GetOptions<TOptions>()
        where TOptions : class, new()
    {
        var options = this.ServiceProvider?.GetService<TOptions>();
        if (options is not null)
        {
            return options;
        }

        if (this.optionTypeToInstance.TryGetValue(typeof(TOptions), out var instance))
        {
            options = instance as TOptions;
        }

        return options;
    }

    /// <summary>
    /// Gets an array of command <see cref="Type"/> which belong to the specified command type.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns>An array of command type.</returns>
    public Type[] GetCommandTypes(Type commandType)
    {
        if (this.CommandDictionary.TryGetValue(commandType, out var array))
        {
            return array;
        }
        else
        {
            return Array.Empty<Type>();
        }
    }

    /// <summary>
    /// Create instances registered by <see cref="IUnitConfigurationContext.RegisterInstanceCreation{T}()"/>.
    /// </summary>
    public void CreateInstances()
    {
        foreach (var x in this.InstanceCreationTypes)
        {
            _ = this.ServiceProvider.GetService(x);
        }
    }

    /// <summary>
    /// Sends a prepare notification to all the units which implement <see cref="IUnitPreparable"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SendPrepare(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitPreparable>().Prepare(this, cancellationToken);

    /// <summary>
    /// Sends a start notification to all the units which implement <see cref="IUnitExecutable"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SendStart(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitExecutable>().Start(this, cancellationToken);

    /// <summary>
    /// Sends a stop notification to all the units which implement <see cref="IUnitExecutable"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SendStop(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitExecutable>().Stop(this, cancellationToken);

    /// <summary>
    /// Sends a terminate notification to all the units which implement <see cref="IUnitExecutable"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SendTerminate(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitExecutable>().Terminate(this, cancellationToken);

    /// <summary>
    /// Sends a load notification to all the units which implement <see cref="IUnitSerializable"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SendLoad(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitSerializable>().Load(this, cancellationToken);

    /// <summary>
    /// Sends a save notification to all the units which implement <see cref="IUnitSerializable"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SendSave(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitSerializable>().Save(this, cancellationToken);

    /// <summary>
    /// Converts <see cref="UnitBuilderContext"/> to <see cref="UnitContext"/>.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceCollection"/>.</param>
    /// <param name="builderContext"><see cref="UnitBuilderContext"/>.</param>
    internal void FromBuilderToUnitContext(IServiceProvider serviceProvider, UnitBuilderContext builderContext)
    {
        this.ServiceProvider = serviceProvider;
        this.optionTypeToInstance = builderContext.OptionTypeToInstance;
        this.Radio = serviceProvider.GetRequiredService<RadioClass>();
        this.InstanceCreationTypes = builderContext.InstanceCreationSet.ToArray();

        this.ExecutionRoot = serviceProvider.GetRequiredService<ExecutionRoot>();
        var options = serviceProvider.GetRequiredService<UnitOptions>();
        options.CopyFrom(builderContext);
        this.Options = options;

        ((IUnitConfigurationAndPostConfigurationContext)builderContext).GetCommandGroup(typeof(UnitBuilderContext.TopCommand));
        ((IUnitConfigurationAndPostConfigurationContext)builderContext).GetCommandGroup(typeof(UnitBuilderContext.SubCommand));
        foreach (var x in builderContext.CommandGroups)
        {
            this.CommandDictionary[x.Key] = x.Value.ToArray();
        }

        this.LoggerResolvers = builderContext.LoggerResolvers.ToArray();
    }

    internal void AddRadio(UnitBase unit)
    {
        if (unit is IUnitPreparable preparable)
        {
            this.Radio.Open(preparable, true);
        }

        if (unit is IUnitExecutable executable)
        {
            this.Radio.Open(executable, true);
        }

        if (unit is IUnitSerializable serializable)
        {
            this.Radio.Open(serializable, true);
        }
    }
}
