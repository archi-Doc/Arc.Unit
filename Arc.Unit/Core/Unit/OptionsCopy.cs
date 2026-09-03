// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Arc.Unit;

/// <summary>
/// Copies the contents of an options instance to another instance of the same type.
/// </summary>
/// <remarks>
/// Options instances are registered in the DI container during the build process, and therefore they cannot be replaced afterwards.<br/>
/// Instead, the values of the new instance (usually created with a <see langword="with"/> expression) are copied into the registered instance.
/// </remarks>
internal static class OptionsCopy
{
    private const BindingFlags DeclaredInstanceFields =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly ConcurrentDictionary<Type, FieldInfo[]> TypeToFields = new();

    /// <summary>
    /// Copies all the instance fields (including the private and inherited fields) from one instance to another.
    /// </summary>
    /// <typeparam name="TOptions">The type of the options instances.</typeparam>
    /// <param name="from">The source instance.</param>
    /// <param name="to">The destination instance.</param>
    internal static void Copy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TOptions>(TOptions from, TOptions to)
        where TOptions : class
    {
        // typeof(TOptions) is used instead of from.GetType(), since 'to' is always created as TOptions
        // (the fields declared by a derived type of 'from' cannot be set to 'to').
        if (!TypeToFields.TryGetValue(typeof(TOptions), out var fields))
        {// GetOrAdd() is not used, since a lambda parameter cannot carry the DynamicallyAccessedMembers annotation.
            fields = GetInstanceFields(typeof(TOptions));
            TypeToFields[typeof(TOptions)] = fields;
        }

        for (var i = 0; i < fields.Length; i++)
        {
            fields[i].SetValue(to, fields[i].GetValue(from));
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern", Justification = "TOptions is annotated with DynamicallyAccessedMemberTypes.All, which preserves the type and its base types along with all their members.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:UnrecognizedReflectionPattern", Justification = "TOptions is annotated with DynamicallyAccessedMemberTypes.All, which preserves the type and its base types along with all their members.")]
    private static FieldInfo[] GetInstanceFields([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        var fields = type.GetFields(DeclaredInstanceFields);
        var baseType = type.BaseType;
        if (baseType is null || baseType == typeof(object))
        {// No base type: the declared fields are the whole set (no list is allocated).
            return fields;
        }

        var list = new List<FieldInfo>(fields);
        for (var t = baseType; t is not null && t != typeof(object); t = t.BaseType)
        {
            list.AddRange(t.GetFields(DeclaredInstanceFields));
        }

        return list.ToArray();
    }
}
