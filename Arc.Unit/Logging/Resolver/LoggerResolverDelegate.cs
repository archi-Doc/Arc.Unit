// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Determines the <see cref="ILogOutput"/> and <see cref="ILogFilter"/> for the log source/level of the specified context.<br/>
/// Resolvers are called in the order they are registered, and the last one determines the result.
/// </summary>
/// <param name="context">The context which holds the log source/level and receives the output/filter.</param>
public delegate void LoggerResolverDelegate(LoggerResolverContext context);
