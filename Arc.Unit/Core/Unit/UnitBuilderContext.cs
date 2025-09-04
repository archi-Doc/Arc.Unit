// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
    public UnitArguments Arguments { get; private set; } = new();

    /// <summary>
    /// Gets <see cref="IServiceCollection"/>.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    public IServiceProvider ServiceProvider { get; internal set; } = default!;

    internal HashSet<Type> CreateInstanceSet { get; } = new();

    internal Dictionary<Type, CommandGroup> CommandGroups { get; } = new();

    internal List<LoggerResolverDelegate> LoggerResolvers { get; } = new();

    internal Dictionary<Type, object> OptionTypeToInstance { get; } = new();

    internal HashSet<UnitBuilder> ProcessedBuilderTypes { get; } = new();

    internal Dictionary<Type, object> CustomContexts { get; } = new();

    #endregion

    public UnitBuilderContext(string? args)
    {
        this.UnitName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;

        // Arguments
        if (args != null)
        {
            this.Arguments.Add(args);
        }

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

    void IUnitPreConfigurationContext.SetOptions<TOptions>(TOptions options)
    {//
        this.OptionTypeToInstance[typeof(TOptions)] = options;
    }

    #endregion

    public bool TryGetOptions<TOptions>([MaybeNullWhen(false)] out TOptions options)
        where TOptions : class
    {
        options = null;
        if (!this.OptionTypeToInstance.TryGetValue(typeof(TOptions), out var instance))
        {
            return false;
        }

        options = instance as TOptions;
        return options != null;
    }

    public void GetOptions<TOptions>(out TOptions options)
        where TOptions : class
    {
        options = (TOptions)this.OptionTypeToInstance[typeof(TOptions)];
    }

    public TOptions GetOrCreateOptions<TOptions>()
        where TOptions : class, new()
    {
        TOptions? options = null;
        if (this.OptionTypeToInstance.TryGetValue(typeof(TOptions), out var instance))
        {
            options = instance as TOptions;
            if (options != null)
            {
                return options;
            }
        }

        options = new();
        this.OptionTypeToInstance[typeof(TOptions)] = options;

        return options;
    }

    public void ClearLoggerResolver() => this.LoggerResolvers.Clear();

    public void AddLoggerResolver(LoggerResolverDelegate resolver) => this.LoggerResolvers.Add(resolver);

    public void CreateInstance<T>() => this.CreateInstanceSet.Add(typeof(T));

    public CommandGroup GetCommandGroup(Type type)
    {
        if (!this.CommandGroups.TryGetValue(type, out var commandGroup))
        {
            this.TryAddSingleton(type);
            commandGroup = new(this);
            this.CommandGroups.Add(type, commandGroup);
        }

        return commandGroup;
    }

    public CommandGroup GetCommandGroup() => this.GetCommandGroup(typeof(TopCommand));

    public CommandGroup GetSubcommandGroup() => this.GetCommandGroup(typeof(SubCommand));

    public bool AddCommand(Type commandType)
    {
        var group = this.GetCommandGroup();
        return group.AddCommand(commandType);
    }

    public bool AddSubcommand(Type commandType)
    {
        var group = this.GetSubcommandGroup();
        return group.AddCommand(commandType);
    }

    [MemberNotNull(nameof(ProgramDirectory), nameof(DataDirectory))]
    internal void SetDirectory()
    {
        if (this.Arguments.TryGetOption(RootDirectoryOptionName, out var value))
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

        if (this.Arguments.TryGetOption(DataDirectoryOptionName, out value))
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
