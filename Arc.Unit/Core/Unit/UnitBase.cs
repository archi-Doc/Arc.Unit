// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Base class of Unit.<br/>
/// <b>Unit = Builder + Product(Instance) + Function</b><br/>
/// By implementing <see cref="IUnitPreparable"/>, <see cref="IUnitExecutable"/> or <see cref="IUnitSerializable"/>,
/// the unit receives the notifications sent by <see cref="UnitContext"/> (e.g. <see cref="UnitContext.SendPrepare(CancellationToken)"/>).
/// </summary>
public abstract class UnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBase"/> class.
    /// </summary>
    /// <param name="context">The <see cref="UnitContext"/> which the unit is registered to.</param>
    public UnitBase(UnitContext context)
    {
        context.AddRadio(this);
    }
}
