## Arc.Unit = Builder + Product(Instance) + Function

[![Nuget](https://img.shields.io/nuget/v/Arc.Unit)](https://www.nuget.org/packages/Arc.Unit/)
[![License](https://img.shields.io/github/license/archi-Doc/Arc.Unit)](https://github.com/archi-Doc/Arc.Unit/blob/main/LICENSE)

**Arc.Unit** is an independent unit of function and dependency (a lightweight alternative to the .NET Generic Host).

- **Builder**: `UnitBuilder` collects configuration delegates and registers services.
- **Product**: `UnitBuilder.Build()` creates the DI container and returns a `UnitProduct`.
- **Function**: classes derived from `UnitBase` receive lifecycle notifications through the shared `UnitContext`.

Features:

- Three configuration phases (pre-configuration, configuration, post-configuration).
- Composable builders: `AddBuilder()` merges another builder, and each builder is processed only once.
- Lifecycle notifications: Prepare, Load, Start, Stop, Save and Terminate.
- Options: obtain and replace option records while building.
- Logging: console/file/memory outputs, per source/level resolvers, and filters.
- Command-line arguments (`UnitArguments`) and command registration for [SimpleCommandLine](https://github.com/archi-Doc/SimpleCommandLine).
- Console abstraction (`IConsoleService`) which can be replaced for tests.

Work in progress.



## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [UnitBuilder](#unitbuilder)
- [UnitBase and the lifecycle](#unitbase-and-the-lifecycle)
- [Options](#options)
- [Logging](#logging)
- [Command-line arguments](#command-line-arguments)
- [Commands](#commands)
- [Console service](#console-service)
- [Native AOT](#native-aot)
- [Samples](#samples)
- [License](#license)



## Installation

```
dotnet add package Arc.Unit
```

Arc.Unit targets .NET 10 and depends on `Arc.Collections`, `Arc.CrossChannel`, `Arc.Threading` and `Microsoft.Extensions.DependencyInjection`.



## Quick Start

```csharp
using Arc.Unit;
using Arc.Threading;
using Microsoft.Extensions.DependencyInjection;

// 1. Create a builder and register the units and services.
var builder = new UnitBuilder()
    .Configure(context =>
    {
        context.AddSingletonUnit<MyUnit>(); // Registers MyUnit and creates it in CreateInstances().
    });

// 2. Build a product (the DI container is created here).
var product = builder.Build(args);
var context = product.Context;

// 3. Create the registered instances and send the notifications.
context.CreateInstances();
await context.SendPrepare();
await context.SendLoad();
await context.SendStart();

// Main processing...

await context.SendSave();
await context.SendStop();
await context.SendTerminate();

// 4. Terminate the background workers and flush the logs.
context.ExecutionRoot.RequestTermination();
await context.ServiceProvider.GetRequiredService<LogUnit>().FlushAndTerminate();
await context.ExecutionRoot.WaitForTermination(TerminationOptions.IncludeIndependent);
```

```csharp
public class MyUnit : UnitBase, IUnitPreparable
{
    private readonly ILogger<MyUnit> logger;

    public MyUnit(UnitContext context, ILogger<MyUnit> logger)
        : base(context) // The base constructor registers this instance for the notifications.
    {
        this.logger = logger;
    }

    Task IUnitPreparable.Prepare(UnitContext context, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit prepared.");
        return Task.CompletedTask;
    }
}
```



## UnitBuilder

`UnitBuilder` runs the registered delegates in three phases, and `Build()` can be called only once.

| Phase | Method | Context | Typical use |
| --- | --- | --- | --- |
| 1. Pre-configuration | `PreConfigure()` | `IUnitPreConfigurationContext` | Read the command-line arguments, set `UnitName`/`ProgramDirectory`/`DataDirectory`, prepare options. |
| 2. Configuration | `Configure()` | `IUnitConfigurationContext` | Register services and units, add commands, set up the loggers. |
| — | — | — | The `IServiceProvider` is created here. |
| 3. Post-configuration | `PostConfigure()` | `IUnitPostConfigurationContext` | Update options with the values determined during the build (the service provider is available). |

Each method can be called multiple times, and all the delegates are combined.

Builders are composable, which is the usual way to publish a reusable unit:

```csharp
public class MyUnit : UnitBase
{
    public class Builder : UnitBuilder<Product>
    {// Builder class for customizing dependencies.
        public Builder()
        {
            this.Configure(context => context.AddSingletonUnit<MyUnit>());
        }
    }

    public class Product : UnitProduct
    {// Product class for customizing behaviors.
        public Product(UnitContext context)
            : base(context)
        {
        }
    }
}

var builder = new MyUnit.Builder()
    .Configure(context => { /* Additional configuration. */ });
builder.AddBuilder(anotherBuilder); // Adding the same builder twice has no additional effect.
MyUnit.Product product = builder.Build();
```

Other members:

- `SetServiceProviderFactory()`: replaces the factory which creates the `IServiceProvider`.
- `GetBuiltProduct()`: returns the product created by `Build()`.
- `UnitBuilderToServiceProviderFactory`: uses a `UnitBuilder` as the `IServiceProviderFactory<TContainerBuilder>` of the .NET Generic Host.

The services registered by default are: `UnitContext`, `UnitOptions`, `ExecutionRoot`, `RadioClass`, `IConsoleService` (`ConsoleService`), the product type, and the logging services described below.



## UnitBase and the lifecycle

- Inherit from **UnitBase** and implement **IUnitPreparable**, **IUnitExecutable** or **IUnitSerializable**.
- Register it with `context.AddSingletonUnit<TUnit>()` (this also registers the type for instance creation).
- Instances are created by `product.Context.CreateInstances()`, and each instance is registered to the notification radio by the `UnitBase` constructor.
- Notify all the units via `product.Context.SendPrepare()` and the other `Send*` methods.

| Interface | Method | Sent by | Description |
| --- | --- | --- | --- |
| `IUnitPreparable` | `Prepare()` | `SendPrepare()` | Called once at the very beginning. |
| `IUnitSerializable` | `Load()` | `SendLoad()` | Called once after `Prepare()`. Throw `PanicException` to abort. |
| `IUnitExecutable` | `Start()` | `SendStart()` | Called after `Load()`; may be called multiple times. |
| `IUnitExecutable` | `Stop()` | `SendStop()` | Called after `Start()`. |
| `IUnitSerializable` | `Save()` | `SendSave()` | May be called multiple times. |
| `IUnitExecutable` | `Terminate()` | `SendTerminate()` | Called once at the beginning of the termination process. |

`UnitContext` also provides `ServiceProvider`, `ExecutionRoot` (the root of the background tasks), `Options` (`UnitOptions`), `Radio`, `Commands`/`Subcommands` and `TerminationRequested`.



## Options

An option is a `record class` with a parameterless constructor. `GetOptions<TOptions>()` returns the instance which is registered in the DI container, and `SetOptions<TOptions>()` copies the values into it (so the instance held by the container stays the same).

```csharp
builder.PostConfigure(context =>
{
    context.SetOptions(context.GetOptions<FileLoggerOptions>() with
    {
        Path = Path.Combine(context.DataDirectory, "Logs/Log.txt"),
        MaxLogCapacity = 2,
    });
});
```

`IUnitPreConfigurationContext.GetCustomContext<TContext>()` provides a shared context (`IUnitCustomContext`) which can carry information between builders. `ProcessContext()` is called after the configuration phase.



## Logging

Inject `ILogger<TLogSource>` (the source type is used as the category of the log), obtain a `LogWriter` for the level, and write the message. `GetWriter()` returns `null` when no output is assigned, so the message is not even created:

```csharp
this.logger.GetWriter()?.Write("Information");
this.logger.GetWriter(LogLevel.Error)?.Write($"Error: {code}");
```

Log levels are `Debug`, `Information` (default), `Warning`, `Error` and `Fatal`.
Use `ILogger<DefaultLog>` (or `ILogger`) to omit the source name from the formatted text.

### Resolvers

A resolver determines the output and the filter for each log source/level pair. Resolvers are called in the order they are registered, and the result is cached.

```csharp
context.ClearLoggerResolver(); // Clears the default resolver (all logs -> ConsoleLogger).
context.AddLoggerResolver(x =>
{// Log source/level -> Resolver() -> Output/filter
    if (x.LogLevel <= LogLevel.Debug)
    {
        x.SetOutput<ConsoleLogger>();
        return;
    }

    x.SetOutput<ConsoleAndFileLogger>();
    if (x.LogSourceType == typeof(MyCommand))
    {
        x.SetFilter<MyLogFilter>(); // The filter type must be registered in the DI container.
    }
});
```

### Outputs

| Output | Description |
| --- | --- |
| `ConsoleLogger` | Writes to the console via `IConsoleService`. Set `ConsoleLoggerOptions.EnableBuffering` to write logs from a background worker. |
| `FileLogger<TOption>` | Writes to a file (one file per day) from a background worker. The oldest files are deleted when the total size exceeds `FileLoggerOptions.MaxLogCapacity`. |
| `ConsoleAndFileLogger` | Writes to both `ConsoleLogger` and `FileLogger<FileLoggerOptions>`. |
| `MemoryLogger` | Keeps the formatted logs in memory (`MemoryLogger.ToUtf8Array()`). |
| `EmptyLogger` | Discards all logs. |

To add another file logger, derive a new options type from `FileLoggerOptions`, register it (`context.TryAddSingleton<MyFileLoggerOptions>()`), and use `FileLogger<MyFileLoggerOptions>` as the output type. The open generic registration creates the logger automatically.

### Filters

A filter is applied before the log is written, and it can change the destination or discard the log.

```csharp
public LogWriter? Filter(LogFilterParameter parameter)
{
    if (parameter.LogLevel == LogLevel.Error)
    {
        return parameter.LogService.GetWriter<ConsoleAndFileLogger>(LogLevel.Fatal); // Error -> Fatal
    }

    return parameter.OriginalWriter; // null to discard the log.
}
```

### Flush

Buffered outputs are written by background workers. Before exiting, flush them and terminate the workers:

```csharp
var logUnit = product.Context.ServiceProvider.GetRequiredService<LogUnit>();
await logUnit.Flush();             // Flushes all the buffered outputs.
await logUnit.FlushConsole();      // Flushes the console output only.
await logUnit.FlushAndTerminate(); // Flushes all the buffered outputs and terminates the workers.
```

`LogUnit.SetTimeOffset()` adjusts the timestamp of the log events, and `SimpleLogFormatterOptions` customizes the format ("Timestamp [Level Source(EventId)] Message").



## Command-line arguments

The arguments passed to `Build()` are parsed into options (prefixed with `-`) and values, and they are available via `IUnitPreConfigurationContext.Arguments`.

```csharp
builder.PreConfigure(context =>
{
    if (context.Arguments.TryGetOptionValue("Mode", out var mode))
    {
        // -mode Test
    }
});
```

- `Build(string[] args)` joins the arguments, and an argument which contains whitespace is enclosed in quotation marks.
- `Build(string? args)` parses a single string, where `"A B"`, `'A B'`, `{A B}` and `"""A B"""` keep the enclosed text as one token.
- Option names are compared case-insensitively, and an option without a value is stored as an empty string.

Two options are handled by the builder itself:

| Option | Description |
| --- | --- |
| `-ProgramDirectory` | The directory where the program is located (the default is the current directory). |
| `-DataDirectory` | The directory used for data storage (the default is empty). |

A relative path is combined with the current directory. The result is exposed as `UnitOptions` (`UnitName`, `ProgramDirectory`, `DataDirectory`).



## Commands

Command types can be registered during the configuration phase. When using [SimpleCommandLine](https://github.com/archi-Doc/SimpleCommandLine), use its generic registration extensions and create the parser from the built unit:

```csharp
context.AddCommand<ExampleCommand, ExampleCommandOptions>();
context.AddSubcommand<ExampleSubcommand>();

var parser = product.Context.CreateSimpleParser(parserOptions);
await parser.ParseAndExecute(args);
```

These methods register the command in the DI container and preserve its command and options metadata for trimming and Native AOT. The default lifetime is `Scoped`. Register every nested options type with `context.AddOptionType<TOptions>()`.

The non-generic `Arc.Unit` methods (`context.AddCommand(typeof(ExampleCommand))` and `AddSubcommand`) remain available for other parsers. `UnitContext.GetCommandTypes(Type)` returns the commands which belong to a specified group.



## Console service

`IConsoleService` abstracts the console input/output, so that the output can be colored, suppressed or captured.

```csharp
consoleService.WriteLine("Text", ConsoleColor.Red);
var result = await consoleService.ReadLine(cancellationToken);
if (result.IsSuccess)
{
    // result.Text
}
```

`ConsoleService` is registered by default, and `EmptyConsole` discards all the output. `ConsoleHelper` provides the escape sequences (colors, cursor and erase operations).



## Native AOT

Arc.Unit is trim-compatible and Native AOT-compatible (`IsAotCompatible`), so an application can be published with:

```
dotnet publish -r win-x64 -p:PublishAot=true
```

The public API is annotated with `DynamicallyAccessedMembers`, so the trimmer preserves what the DI container needs. Two points to keep in mind:

- **Open generic services must be closed with reference types.** `Microsoft.Extensions.DependencyInjection` cannot create a generic service with a value type argument on Native AOT, so a log source (`ILogger<TLogSource>`) and any open generic registration of your own must be closed with a class or an interface, not with a struct.
- **Register commands and their options statically.** With SimpleCommandLine, use `AddCommand<TCommand, TOptions>()`/`AddSubcommand<TCommand, TOptions>()`, register nested options with `AddOptionType<TOptions>()`, and create the parser with `UnitContext.CreateSimpleParser()`. The runtime type-discovery overloads are not trim-safe.



## Samples

- [QuickStart](/QuickStart): a console application with commands, a log filter and a file logger.
- [Playground](/Playground): a sandbox which exercises the builder, the loggers and the termination process.



## License

Arc.Unit is licensed under the [MIT License](/LICENSE).
