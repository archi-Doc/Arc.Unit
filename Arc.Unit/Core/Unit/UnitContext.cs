// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;
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
    /// Gets or sets a value indicating whether an exit has been requested for the current unit context.
    /// </summary>
    public bool ExitRequested { get; set; }

    /// <summary>
    /// Gets an instance of <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; } = default!;

    /// <summary>
    /// Gets the <see cref="UnitCore"/> associated with this context.
    /// </summary>
    public UnitCore Core { get; private set; } = default!;

    /// <summary>
    /// Gets the <see cref="UnitOptions"/> associated with this context.
    /// </summary>
    public UnitOptions Options { get; private set; } = new();

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

    public Task SendPrepare(UnitMessage.Prepare message)
        => this.Radio.Send<IUnitPreparable>().Prepare(message);

    public Task SendStart(UnitMessage.Start message, CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitExecutable>().Start(message, cancellationToken);

    public Task SendStop(UnitMessage.Stop message)
        => this.Radio.Send<IUnitExecutable>().Stop(message);

    public Task SendTerminate(UnitMessage.Terminate message, CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitExecutable>().Terminate(message, cancellationToken);

    public Task SendLoad(UnitMessage.Load message, CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitSerializable>().Load(message, cancellationToken);

    public Task SendSave(UnitMessage.Save message, CancellationToken cancellationToken = default)
        => this.Radio.Send<IUnitSerializable>().Save(message, cancellationToken);

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

        this.Core = serviceProvider.GetRequiredService<UnitCore>();
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
