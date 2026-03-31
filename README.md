## Arc.Unit = Builder + Product(Instance) + Function
Arc.Unit is an independent unit of function and dependency (like .Net Generic Host).

Work in progress



## UnitBase

- Inherit from **UnitBase** and implement interfaces such as **IUnitPreparable**, **IUnitExecutable**, and **IUnitSerializable**.
- Register it with `Context.AddSingletonUnit<TUnit>()`.

Then:

- Instances are created by `Unit.Context.CreateInstances()`.
- You can notify all Units via `Unit.Context.SendPrepare()`.
