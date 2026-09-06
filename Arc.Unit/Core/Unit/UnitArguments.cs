// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Arc.Unit;

/// <summary>
/// Parses and stores command-line arguments, supporting both options (prefixed with '-') and values.
/// </summary>
public class UnitArguments
{
    private const char Separator = '|';
    private const char OptionPrefix = '-';
    private const char Quote = '\"';
    private const char OpenBracket = '{'; // '['
    private const char CloseBracket = '}'; // ']'
    private const char SingleQuote = '\'';

    private const StringComparison DefaultStringComparison = StringComparison.OrdinalIgnoreCase;

    #region FieldAndProperty

    private readonly List<string> values = new();
    private readonly List<KeyValuePair<string, string>> options = new();
    private readonly string[]? argumentArray;
    private string? rawArguments;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitArguments"/> class with the specified argument string.
    /// </summary>
    /// <param name="args">The raw argument string to parse.</param>
    internal UnitArguments(string? args)
    {
        this.Initialize(args);
    }

    internal UnitArguments(string[]? args)
    {
        this.argumentArray = args is null ? [] : (string[])args.Clone();
        string? previousOption = null;
        foreach (var token in this.argumentArray)
        {
            ArgumentNullException.ThrowIfNull(token);
            if (IsOptionString(token))
            {
                if (previousOption is not null)
                {
                    this.options.Add(new(previousOption, string.Empty));
                }

                previousOption = token.Trim('-');
            }
            else if (previousOption is not null)
            {
                this.options.Add(new(previousOption, token));
                previousOption = null;
            }
            else
            {
                this.values.Add(token);
            }
        }

        if (previousOption is not null)
        {
            this.options.Add(new(previousOption, string.Empty));
        }
    }

    /// <summary>
    /// Gets the original string, or a space-joined display of pre-split arguments (not a reversible encoding).
    /// </summary>
    public string RawArguments => this.rawArguments ??= this.argumentArray is null ? string.Empty : string.Join(' ', this.argumentArray);

    /// <summary>
    /// Attempts to get the value associated with the specified option name.
    /// </summary>
    /// <param name="optionName">The name of the option to search for.</param>
    /// <param name="optionValue">When this method returns, contains the value associated with the option, if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the option was found; otherwise, <c>false</c>.</returns>
    public bool TryGetOptionValue(string optionName, [MaybeNullWhen(false)] out string optionValue)
    {
        var span = CollectionsMarshal.AsSpan(this.options);
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i].Key.Equals(optionName, DefaultStringComparison))
            {
                optionValue = span[i].Value;
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
        => this.TryGetOptionValue(option, out _);

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

    private static bool IsOptionString(string text) => text.StartsWith(OptionPrefix);

    private static List<Range> FormatArguments(string arg)
    {
        var span = arg.AsSpan();
        var list = new List<Range>();

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
                    (peek == '3' || peek == OpenBracket) &&
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
                        enclosed.Push('3');
                        position += 2;
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
                    else if (peek == '3' || peek == SingleQuote)
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
                    else if (peek == '3' || peek == Quote)
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
                var s = span[start..position].Trim();
                if (s.Length > 0)
                {
                    list.Add(start..position);
                }
            }

            if (currentChar == Separator)
            {
                list.Add(position..(position + 1));
                position++;
                nextPosition++;
            }

            start = position;
            position = nextPosition;
        }

        if (start < position && position <= span.Length)
        { // Add string
            var s = span[start..position].Trim();
            if (s.Length > 0)
            {
                list.Add(start..position);
            }
        }

        return list;
    }

    private void Initialize(string? args)
    {
        if (string.IsNullOrEmpty(args))
        {
            return;
        }

        this.rawArguments = args;
        string? previousOption = null;

        foreach (var range in FormatArguments(args))
        {
            var x = args.AsSpan(range).Trim();
            if (x[0] == OptionPrefix)
            {// -option
                if (previousOption != null)
                {
                    AddOptionAndValue(previousOption, string.Empty); // Previous option
                    previousOption = null;
                }

                previousOption = x.Trim('-').ToString();
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
            this.options.Add(new(option, value));
        }

        static string ProcessValueString(ReadOnlySpan<char> value)
        {
            if (value.Length >= 6 && value.StartsWith("\"\"\"", StringComparison.Ordinal) && value.EndsWith("\"\"\"", StringComparison.Ordinal))
            {
                return value[3..^3].ToString();
            }

            if (value.Length >= 2 && value[0] == Quote && value[^1] == Quote)
            {
                return value[1..^1].ToString();
            }
            else if (value.Length >= 2 && value[0] == SingleQuote && value[^1] == SingleQuote)
            {
                return value[1..^1].ToString();
            }
            else
            {
                return value.ToString();
            }
        }
    }
}
