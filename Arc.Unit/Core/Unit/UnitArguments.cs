// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using SimpleCommandLine;

namespace Arc.Unit;

/// <summary>
/// Parses and stores command-line arguments, supporting both options (prefixed with '-') and values.
/// </summary>
public class UnitArguments
{
    private static readonly StringComparison DefaultStringComparison = StringComparison.InvariantCultureIgnoreCase;

    #region FieldAndProperty

    private string rawArguments = string.Empty;
    private List<string> values = new();
    private List<KeyValuePair<string, string>> options = new();

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitArguments"/> class with the specified argument string.
    /// </summary>
    /// <param name="args">The raw argument string to parse.</param>
    internal UnitArguments(string? args)
    {
        this.Initialize(args);
    }

    /// <summary>
    /// Gets the raw argument string as provided.
    /// </summary>
    public string RawArguments => this.rawArguments;

    /// <summary>
    /// Attempts to get the value associated with the specified option name.
    /// </summary>
    /// <param name="optionName">The name of the option to search for.</param>
    /// <param name="optionValue">When this method returns, contains the value associated with the option, if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the option was found; otherwise, <c>false</c>.</returns>
    public bool TryGetOptionValue(string optionName, [MaybeNullWhen(false)] out string optionValue)
    {
        foreach (var x in this.options)
        {
            if (x.Key.Equals(optionName, DefaultStringComparison))
            {
                optionValue = x.Value;
                return true;
            }
        }

        optionValue = default;
        return false;
    }

    /// <summary>
    /// Determines whether the specified option exists in the argument list.
    /// </summary>
    /// <param name="option">The option name to check for.</param>
    /// <returns><c>true</c> if the option exists; otherwise, <c>false</c>.</returns>
    public bool ContainsOption(string option)
    {
        return this.options.Any(x => x.Key.Equals(option, DefaultStringComparison));
    }

    /// <summary>
    /// Determines whether the specified value exists in the argument list.
    /// </summary>
    /// <param name="value">The value to check for.</param>
    /// <returns><c>true</c> if the value exists; otherwise, <c>false</c>.</returns>
    public bool ContainsValue(string value) => this.values.Contains(value);

    /// <summary>
    /// Gets an enumerable collection of all values (non-option arguments).
    /// </summary>
    /// <returns>An enumerable of argument values.</returns>
    public IEnumerable<string> GetValues() => this.values;

    /// <summary>
    /// Gets an enumerable collection of all options and their associated values.
    /// </summary>
    /// <returns>An enumerable of option name and value pairs.</returns>
    public IEnumerable<(string OptionName, string OptionValue)> GetOptions()
    {
        foreach (var x in this.options)
        {
            yield return (x.Key, x.Value);
        }
    }

    internal void Initialize(string? args)
    {
        if (string.IsNullOrEmpty(args))
        {
            return;
        }

        this.rawArguments = args;
        string? previousOption = null;

        foreach (var x in args.FormatArguments())
        {
            if (x.IsOptionString())
            {// -option
                if (previousOption != null)
                {
                    AddOptionAndValue(previousOption, string.Empty); // Previous option
                    previousOption = null;
                }

                previousOption = x.Trim('-');
            }
            else
            {// value
                if (previousOption != null)
                {// -option value
                    AddOptionAndValue(previousOption, ProcessValueString(x));
                    previousOption = null;
                }
                else
                {// value
                    this.values.Add(ProcessValueString(x));
                }
            }
        }

        if (previousOption != null)
        {
            AddOptionAndValue(previousOption, string.Empty); // Previous option
        }

        void AddOptionAndValue(string option, string value)
        {
            // option = option.ToLower();
            this.options.Add(new(option, value));
        }

        static string ProcessValueString(string value)
        {
            if (value.Length >= 2 && value.StartsWith('\"') && value.EndsWith('\"'))
            {
                return value.Substring(1, value.Length - 2);
            }
            else if (value.Length >= 2 && value.StartsWith('\'') && value.EndsWith('\''))
            {
                return value.Substring(1, value.Length - 2);
            }
            else
            {
                return value;
            }
        }
    }
}
