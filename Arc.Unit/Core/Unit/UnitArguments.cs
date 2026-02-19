// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Arc.Unit;

/// <summary>
/// Parses and stores command-line arguments, supporting both options (prefixed with '-') and values.
/// </summary>
public class UnitArguments
{
    private const char Separator = '|';
    private const string SeparatorString = "|";
    private const char OptionPrefix = '-';
    private const char Quote = '\"';
    private const char OpenBracket = '{'; // '['
    private const char CloseBracket = '}'; // ']'
    private const char SingleQuote = '\'';

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

        foreach (var x in FormatArguments(args))
        {
            if (IsOptionString(x))
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

    private static bool IsOptionString(string text) => text.StartsWith(OptionPrefix);

    private static string[] FormatArguments(string arg)
    {
        var span = arg.AsSpan();
        var list = new List<string>();

        var start = 0;
        var position = 0;
        var nextPosition = 0;
        var enclosed = new Stack<char>();
        while (position < span.Length)
        {
            var currentChar = span[position];
            var lastChar = position > 0 ? span[position - 1] : (char)0;
            if (enclosed.Count == 0)
            {
                if (char.IsWhiteSpace(currentChar))
                {// A B
                    nextPosition = position + 1;
                    goto AddString;
                }
                else if (currentChar == Separator)
                {
                    nextPosition = position;
                    goto AddString;
                }
                else if (currentChar == Quote &&
                    (position + 2) < span.Length &&
                    span[position + 1] == Quote &&
                    span[position + 2] == Quote)
                {// """A B"""
                    enclosed.Push('3');
                    nextPosition = position + 3;
                    goto AddString;
                }
                else if (currentChar == OpenBracket ||
                    (currentChar == Quote && lastChar != '\\') ||
                    (currentChar == SingleQuote && lastChar != '\\'))
                {// { or " (not \") or '" (not \')
                    enclosed.Push(currentChar);
                    nextPosition = position + 1;
                    goto AddString;
                }
                else if (currentChar == CloseBracket)
                {// }
                    nextPosition = position + 1;
                    goto AddString;
                }
            }
            else
            {
                var peek = enclosed.Peek();

                if (currentChar == Quote &&
                    (position + 2) < span.Length &&
                    span[position + 1] == Quote &&
                    span[position + 2] == Quote)
                {// """
                    var index = 3;
                    while ((position + index) < span.Length &&
                        span[position + index] == Quote)
                    {
                        index++;
                    }

                    if (enclosed.Peek() == '3')
                    {// """abc"""
                        enclosed.Pop();
                        if (enclosed.Count == 0)
                        {
                            position += index;
                            nextPosition = position;
                            goto AddString;
                        }
                    }
                    else
                    {// { """A
                        enclosed.Push(currentChar);
                    }
                }
                else if (currentChar == Quote && lastChar != '\\')
                {// " (not \")
                    if (peek == Quote)
                    {// "-arg {-test "A"} "
                        enclosed.Pop();
                        if (enclosed.Count == 0)
                        {
                            nextPosition = ++position;
                            goto AddString;
                        }
                    }
                    else if (peek == '3')
                    {
                    }
                    else
                    {
                        enclosed.Push(currentChar);
                    }
                }
                else if (currentChar == SingleQuote && lastChar != '\\')
                {// ' (not \')
                    if (peek == SingleQuote)
                    {// '-arg {-test "A"} '
                        enclosed.Pop();
                        if (enclosed.Count == 0)
                        {
                            nextPosition = ++position;
                            goto AddString;
                        }
                    }
                    else if (peek == '3')
                    {
                    }
                    else
                    {
                        enclosed.Push(currentChar);
                    }
                }
                else if (currentChar == CloseBracket)
                {// }
                    if (peek == OpenBracket)
                    {// {-test "A"}
                        enclosed.Pop();
                        if (enclosed.Count == 0)
                        {
                            nextPosition = ++position;
                            goto AddString;
                        }
                    }
                }
                else if (currentChar == OpenBracket)
                {
                    if (peek == OpenBracket)
                    {
                        enclosed.Push(currentChar);
                    }
                }
            }

            position++;
            continue;

AddString:
            if (start < position)
            { // Add string
                var s = span[start..position].ToString().Trim();
                if (s.Length > 0)
                {
                    list.Add(s);
                }
            }

            if (currentChar == Separator)
            {
                list.Add(SeparatorString);
                position++;
                nextPosition++;
            }

            start = position;
            position = nextPosition;
        }

        if (start < position && position <= span.Length)
        { // Add string
            var s = span[start..position].ToString().Trim();
            if (s.Length > 0)
            {
                list.Add(s);
            }
        }

        return list.ToArray();
    }
}
