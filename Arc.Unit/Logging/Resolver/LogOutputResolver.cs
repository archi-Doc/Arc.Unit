// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Determines the <see cref="ILogOutput"/> and <see cref="ILogFilter"/> for the log source/level of the specified context.<br/>
/// Resolvers share the context in registration order; later assignments override earlier ones.
/// Resolution may run concurrently for a cache miss, so delegates must be thread-safe.
/// </summary>
/// <param name="context">The context which holds the log source/level and receives the output/filter.</param>
public delegate void LogOutputResolver(LogOutputResolverContext context);
