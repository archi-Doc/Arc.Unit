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
    /// Gets or sets a value indicating whether the application requested termination.
    /// This flag does not cancel ExecutionRoot or send notifications.
    /// </summary>
    public bool IsTerminationRequested { get; set; }

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
    /// Gets the <see cref="LocalRadio"/> which delivers the notifications (Prepare/Start/Stop/Terminate/Load/Save) to the units.
    /// </summary>
    public LocalRadio Radio { get; private set; } = default!;

    /// <summary>
    /// Gets an array of <see cref="Type"/> registered by <see cref="IUnitConfigurationContext.RegisterInstanceCreation{T}()"/>.<br/>
    /// Note that instances are actually created by calling <see cref="UnitContext.CreateInstances()"/>.
    /// </summary>
    public Type[] InstanceCreationTypes { get; private set; } = [];

    /// <summary>
    /// Gets an array of command <see cref="Type"/> added by <see cref="IUnitConfigurationContext.AddCommand(Type, ServiceLifetime)"/>.
    /// </summary>
    public Type[] CommandTypes => this.CommandTypesByGroup[typeof(UnitBuilderContext.TopCommand)];

    /// <summary>
    /// Gets an array of subcommand <see cref="Type"/> added by <see cref="IUnitConfigurationContext.AddSubcommand(Type, ServiceLifetime)"/>.
    /// </summary>
    public Type[] SubcommandTypes => this.CommandTypesByGroup[typeof(UnitBuilderContext.SubCommand)];

    /// <summary>
    /// Gets a collection of group <see cref="Type"/> (keys, e.g. a parent command type) and the command <see cref="Type"/> which belong to the group (values).
    /// </summary>
    public Dictionary<Type, Type[]> CommandTypesByGroup { get; private set; } = new();

    /// <summary>
    /// Gets an array of <see cref="LogOutputResolver"/> registered by <see cref="IUnitConfigurationContext.AddLogOutputResolver(LogOutputResolver)"/>.
    /// </summary>
    public LogOutputResolver[] LogOutputResolvers { get; private set; } = [];

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
    /// Gets an array of command <see cref="Type"/> which belong to the specified group.
    /// </summary>
    /// <param name="groupType">The type which identifies the group (see <see cref="IUnitCommandContext.GetCommandGroup(Type)"/>).</param>
    /// <returns>An array of command type.</returns>
    public Type[] GetCommandTypes(Type groupType)
    {
        if (this.CommandTypesByGroup.TryGetValue(groupType, out var array))
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
    public Task SendPrepareAsync(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitPreparable>().PrepareAsync(this, cancellationToken);

    /// <summary>
    /// Sends a start notification to all the units which implement <see cref="IUnitExecutable"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SendStartAsync(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitExecutable>().StartAsync(this, cancellationToken);

    /// <summary>
    /// Sends a stop notification to all the units which implement <see cref="IUnitExecutable"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SendStopAsync(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitExecutable>().StopAsync(this, cancellationToken);

    /// <summary>
    /// Sends a terminate notification to all the units which implement <see cref="IUnitExecutable"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SendTerminateAsync(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitExecutable>().TerminateAsync(this, cancellationToken);

    /// <summary>
    /// Sends a load notification to all the units which implement <see cref="IUnitPersistable"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SendLoadAsync(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitPersistable>().LoadAsync(this, cancellationToken);

    /// <summary>
    /// Sends a save notification to all the units which implement <see cref="IUnitPersistable"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task SendSaveAsync(CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitPersistable>().SaveAsync(this, cancellationToken);

    /// <summary>
    /// Converts <see cref="UnitBuilderContext"/> to <see cref="UnitContext"/>.
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <param name="builderContext"><see cref="UnitBuilderContext"/>.</param>
    internal void FromBuilderToUnitContext(IServiceProvider serviceProvider, UnitBuilderContext builderContext)
    {
        this.ServiceProvider = serviceProvider;
        this.optionTypeToInstance = builderContext.OptionTypeToInstance;
        this.Radio = serviceProvider.GetRequiredService<LocalRadio>();
        this.InstanceCreationTypes = builderContext.InstanceCreationSet.ToArray();

        this.ExecutionRoot = serviceProvider.GetRequiredService<ExecutionRoot>();
        var options = serviceProvider.GetRequiredService<UnitOptions>();
        options.CopyFrom(builderContext);
        this.Options = options;

        ((IUnitCommandContext)builderContext).GetCommandGroup(typeof(UnitBuilderContext.TopCommand));
        ((IUnitCommandContext)builderContext).GetCommandGroup(typeof(UnitBuilderContext.SubCommand));
        foreach (var x in builderContext.CommandGroups)
        {
            this.CommandTypesByGroup[x.Key] = x.Value.ToArray();
        }

        this.LogOutputResolvers = builderContext.LogOutputResolvers.ToArray();
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

        if (unit is IUnitPersistable persistable)
        {
            this.Radio.Open(persistable, true);
        }
    }
}
