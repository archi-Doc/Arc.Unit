// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Provides contextual information throughout the entire unit build process (PreConfigure, Configure, PostConfigure).
/// </summary>
internal class UnitBuilderContext : IUnitPreConfigurationContext, IUnitConfigurationContext, IUnitPostConfigurationContext
{
    public const string RootDirectoryOptionName = "ProgramDirectory";
    public const string DataDirectoryOptionName = "DataDirectory";

    /// <summary>
    /// Represents the top-level command group used for identifying types in the unit build process.
    /// </summary>
    internal class TopCommand
    {
    }

    /// <summary>
    /// Represents a subcommand group used for identifying types in the unit build process.
    /// </summary>
    internal class SubCommand
    {
    }

    #region FieldAndProperty

    // public bool IsFirstBuilderRun { get; set; }

    /// <summary>
    /// Gets or sets a unit name.
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>
    /// Gets or sets a program directory.
    /// </summary>
    public string ProgramDirectory { get; set; }

    /// <summary>
    /// Gets or sets a data directory.
    /// </summary>
    public string DataDirectory { get; set; }

    /// <summary>
    /// Gets command-line arguments.
    /// </summary>
    public UnitArguments Arguments { get; private set; }

    /// <summary>
    /// Gets <see cref="IServiceCollection"/>.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    public IServiceProvider ServiceProvider { get; internal set; } = default!;

    internal HashSet<Type> InstanceCreationSet { get; } = new();

    internal Dictionary<Type, CommandGroup> CommandGroups { get; } = new();

    internal List<LoggerResolverDelegate> LoggerResolvers { get; } = new();

    internal Dictionary<Type, object> OptionTypeToInstance { get; } = new();

    internal HashSet<UnitBuilder> ProcessedBuilderTypes { get; } = new();

    internal Dictionary<Type, object> CustomContexts { get; } = new();

    #endregion

    public UnitBuilderContext(string? args)
    {
        this.UnitName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
        this.Arguments = new(args); // Arguments
        this.SetDirectory(); // Directory
    }

    #region IUnitPreConfigurationContext

    TContext IUnitPreConfigurationContext.GetCustomContext<TContext>()
    {
        if (!this.CustomContexts.TryGetValue(typeof(TContext), out var context))
        {
            context = new TContext();
            this.CustomContexts[typeof(TContext)] = context;
        }

        return (TContext)context;
    }

    TOptions IUnitPreConfigurationContext.GetOptions<TOptions>()
    {
        var options = this.ServiceProvider?.GetService<TOptions>();
        if (options is not null)
        {
            return options;
        }

        if (this.OptionTypeToInstance.TryGetValue(typeof(TOptions), out var instance))
        {
            options = instance as TOptions;
            if (options is not null)
            {
                return options;
            }
        }

        options ??= new();
        this.OptionTypeToInstance.TryAdd(typeof(TOptions), options);
        return options;
    }

    void IUnitPreConfigurationContext.SetOptions<TOptions>(TOptions options)
    {
        var baseOptions = ((IUnitPreConfigurationContext)this).GetOptions<TOptions>();
        if (baseOptions != options)
        {
            GhostCopy.Copy(ref options, ref baseOptions);
        }
    }

    #endregion

    #region IUnitConfigurationContext

    void IUnitConfigurationContext.ClearLoggerResolver() => this.LoggerResolvers.Clear();

    void IUnitConfigurationContext.AddLoggerResolver(LoggerResolverDelegate resolver) => this.LoggerResolvers.Add(resolver);

    void IUnitConfigurationContext.RegisterInstanceCreation<T>() => this.InstanceCreationSet.Add(typeof(T));

    bool IUnitConfigurationContext.AddCommand(Type commandType)
    {
        var group = ((IUnitConfigurationAndPostConfigurationContext)this).GetCommandGroup();
        return group.AddCommand(commandType);
    }

    bool IUnitConfigurationContext.AddSubcommand(Type commandType)
    {
        var group = ((IUnitConfigurationAndPostConfigurationContext)this).GetSubcommandGroup();
        return group.AddCommand(commandType);
    }

    #endregion

    #region IUnitConfigurationAndPreConfigurationContext

    CommandGroup IUnitConfigurationAndPostConfigurationContext.GetCommandGroup(Type type)
    {
        if (!this.CommandGroups.TryGetValue(type, out var commandGroup))
        {
            this.TryAddSingleton(type);
            commandGroup = new(this);
            this.CommandGroups.Add(type, commandGroup);
        }

        return commandGroup;
    }

    CommandGroup IUnitConfigurationAndPostConfigurationContext.GetCommandGroup() => ((IUnitConfigurationAndPostConfigurationContext)this).GetCommandGroup(typeof(TopCommand));

    CommandGroup IUnitConfigurationAndPostConfigurationContext.GetSubcommandGroup() => ((IUnitConfigurationAndPostConfigurationContext)this).GetCommandGroup(typeof(SubCommand));

    #endregion

    [MemberNotNull(nameof(ProgramDirectory), nameof(DataDirectory))]
    internal void SetDirectory()
    {
        if (this.Arguments.TryGetOptionValue(RootDirectoryOptionName, out var value))
        {// Root Directory
            if (Path.IsPathRooted(value))
            {
                this.ProgramDirectory = value;
            }
            else
            {
                this.ProgramDirectory = Path.Combine(Directory.GetCurrentDirectory(), value);
            }
        }
        else
        {
            this.ProgramDirectory = Directory.GetCurrentDirectory();
        }

        if (this.Arguments.TryGetOptionValue(DataDirectoryOptionName, out value))
        {// Data Directory
            if (Path.IsPathRooted(value))
            {
                this.DataDirectory = value;
            }
            else
            {
                this.DataDirectory = Path.Combine(Directory.GetCurrentDirectory(), value);
            }
        }
        else
        {
            this.DataDirectory = string.Empty;
        }
    }
}
