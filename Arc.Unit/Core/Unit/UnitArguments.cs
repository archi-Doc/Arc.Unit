// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using SimpleCommandLine;

namespace Arc.Unit;

/// <summary>
/// Manages command line arguments..
/// </summary>
public class UnitArguments
{
    private static readonly StringComparison DefaultStringComparison = StringComparison.InvariantCultureIgnoreCase;

    public UnitArguments(string? args)
    {
        this.Initialize(args);
    }

    public string RawArguments => this.rawArguments;

    public bool TryGetOptionValue(string optionName, [MaybeNullWhen(false)] out string optionValue)
    {// this.options.TryGetValue(option.ToLower(), out value);
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

    public bool ContainsOption(string option)
    {// this.options.ContainsKey(option.ToLower());
        return this.options.Any(x => x.Key.Equals(option, DefaultStringComparison));
    }

    public bool ContainsValue(string value) => this.values.Contains(value);

    public IEnumerable<string> GetValues() => this.values;

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

    private string rawArguments = string.Empty;
    private List<string> values = new();
    private List<KeyValuePair<string, string>> options = new();
}
