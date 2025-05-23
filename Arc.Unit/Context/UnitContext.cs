﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitContext"/> class.
    /// </summary>
    public UnitContext()
    {
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
    /// Create instances registered by <see cref="UnitBuilderContext.CreateInstance{T}()"/>.
    /// </summary>
    public void CreateInstances()
    {
        foreach (var x in this.CreateInstanceTypes)
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
    public Type[] CreateInstanceTypes { get; private set; } = default!;

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

    /// <summary>
    /// Converts <see cref="UnitBuilderContext"/> to <see cref="UnitContext"/>.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceCollection"/>.</param>
    /// <param name="builderContext"><see cref="UnitBuilderContext"/>.</param>
    internal void FromBuilderToUnit(IServiceProvider serviceProvider, UnitBuilderContext builderContext)
    {
        this.ServiceProvider = serviceProvider;
        this.Radio = serviceProvider.GetRequiredService<RadioClass>();
        this.CreateInstanceTypes = builderContext.CreateInstanceSet.ToArray();

        builderContext.GetCommandGroup(typeof(UnitBuilderContext.TopCommand));
        builderContext.GetCommandGroup(typeof(UnitBuilderContext.SubCommand));
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
