// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Contextual information used by Setup delegate and provided to <see cref="UnitBuilder"/>.
/// </summary>
public interface IUnitPostConfigurationContext : IUnitConfigurationContext
{
    /// <summary>
    /// Gets <see cref="IServiceProvider"/>.
    /// </summary>
    IServiceProvider ServiceProvider { get; }
}
