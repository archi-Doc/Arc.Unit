// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrossChannel;

namespace Arc.Unit;

/// <summary>
/// Base class of Unit.<br/>
/// <b>Unit = Builder + Product(Instance) + Function</b><br/>
/// By implementing <see cref="IUnitPreparable"/> and other interfaces, methods can be called from <see cref="UnitContext"/>.
/// </summary>
public abstract class UnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBase"/> class.
    /// </summary>
    /// <param name="context"><see cref="UnitContext"/>.</param>
    public UnitBase(UnitContext context)
    {
        context.AddRadio(this);
    }
}
