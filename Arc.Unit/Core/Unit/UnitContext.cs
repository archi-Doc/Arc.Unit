// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Contextual information provided to <see cref="UnitBase"/>.<br/>
/// In terms of DI, you should avoid using <see cref="UnitContext"/> if possible.
/// </summary>
public sealed class UnitContext
{
    #region FieldAndProperty

    /// <summary>
    /// Gets an instance of <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; } = default!;

    /// <summary>
    /// Gets an instance of <see cref="RadioClass"/>.
    /// </summary>
    public RadioClass Radio { get; private set; } = default!;

    /// <summary>
    /// Gets an array of <see cref="Type"/> which is registered in the creation list.<br/>
    /// Note that instances are actually created by calling <see cref="UnitContext.CreateInstances()"/>.
    /// </summary>
    public Type[] InstanceCreationTypes { get; private set; } = default!;

    /// <summary>
    /// Gets an array of command <see cref="Type"/>.
    /// </summary>
    public Type[] Commands => this.CommandDictionary[typeof(UnitBuilderContext.TopCommand)];

    /// <summary>
    /// Gets an array of subcommand <see cref="Type"/>.
    /// </summary>
    public Type[] Subcommands => this.CommandDictionary[typeof(UnitBuilderContext.SubCommand)];

    /// <summary>
    /// Gets a collection of command <see cref="Type"/> (keys) and subcommand <see cref="Type"/> (values).
    /// </summary>
    public Dictionary<Type, Type[]> CommandDictionary { get; private set; } = new();

    public LoggerResolverDelegate[] LoggerResolvers { get; private set; } = Array.Empty<LoggerResolverDelegate>();

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
            this.ServiceProvider.GetService(x);
        }
    }

    public void SendPrepare(UnitMessage.Prepare message)
        => this.Radio.Send<IUnitPreparable>().Prepare(message);

    public async Task SendStartAsync(UnitMessage.StartAsync message, CancellationToken cancellationToken = default)
        => await this.Radio.Send<IUnitExecutable>().StartAsync(message, cancellationToken).ConfigureAwait(false);

    public void SendStop(UnitMessage.Stop message)
        => this.Radio.Send<IUnitExecutable>().Stop(message);

    public async Task SendTerminateAsync(UnitMessage.TerminateAsync message, CancellationToken cancellationToken = default)
        => await this.Radio.Send<IUnitExecutable>().TerminateAsync(message, cancellationToken).ConfigureAwait(false);

    public async Task SendLoadAsync(UnitMessage.LoadAsync message, CancellationToken cancellationToken = default)
        => await this.Radio.Send<IUnitSerializable>().LoadAsync(message, cancellationToken).ConfigureAwait(false);

    public async Task SendSaveAsync(UnitMessage.SaveAsync message, CancellationToken cancellationToken = default)
        => await this.Radio.Send<IUnitSerializable>().SaveAsync(message, cancellationToken).ConfigureAwait(false);

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
