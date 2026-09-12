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
- Options: update shared option objects without replacing injected instances.
- Logging: console/file/memory outputs, per source/level resolvers, and filters.
- Command-line arguments (`UnitArguments`) and command registration for [SimpleCommandLine](https://github.com/archi-Doc/SimpleCommandLine).
- Console abstraction (`IConsoleService`) which can be replaced for tests.

Work in progress.

API names were revised in this version. See [Arc.Unit Renaming](/doc/Arc.Unit%20Renaming.md) to migrate existing code.



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
- [Performance and concurrency](#performance-and-concurrency)
- [Build and tests](#build-and-tests)
- [License](#license)



## Installation

```
dotnet add package Arc.Unit
```

Arc.Unit targets .NET 10 and depends on `Arc.Collections`, `Arc.CrossChannel`, `Arc.Threading`, `Microsoft.Extensions.DependencyInjection` and `Utf8StringInterpolation`. SimpleCommandLine is an optional integration used by QuickStart.



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
await context.SendPrepareAsync();
await context.SendLoadAsync();
await context.SendStartAsync();

// Main processing...

await context.SendSaveAsync();
await context.SendStopAsync();
await context.SendTerminateAsync();

// 4. Terminate the background workers and flush the logs.
context.ExecutionRoot.RequestTermination();
await context.ServiceProvider.GetRequiredService<LogUnit>().FlushAndTerminateAsync();
await context.ExecutionRoot.WaitForTerminationAsync(TerminationOptions.IncludeIndependent);
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

    Task IUnitPreparable.PrepareAsync(UnitContext context, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit prepared.");
        return Task.CompletedTask;
    }
}
```



## UnitBuilder

`UnitBuilder` runs the registered delegates in three phases. One successful `Build()` is allowed; concurrent and reentrant builds throw. Configure a builder from one thread. A failed build can be retried, but configuration callbacks run again.

| Phase | Method | Context | Typical use |
| --- | --- | --- | --- |
| 1. Pre-configuration | `PreConfigure()` | `IUnitPreConfigurationContext` | Read the command-line arguments, set `UnitName`/`ProgramDirectory`/`DataDirectory`, prepare options. |
| 2. Configuration | `Configure()` | `IUnitConfigurationContext` | Register services and units, add commands, set up the log outputs. |
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
- `UnitServiceProviderFactory`: uses a `UnitBuilder` as the `IServiceProviderFactory<TContainerBuilder>` of the .NET Generic Host.

The services registered by default are: `UnitContext`, `UnitOptions`, `ExecutionRoot`, `RadioClass`, `IConsoleService` (`ConsoleService`), the product type, and the logging services described below.

Changes to `UnitName`, `ProgramDirectory` and `DataDirectory` in `PostConfigure` are reflected in `UnitOptions` when the build completes. Register all services and commands during `Configure`; adding registrations after the provider is built cannot change that provider.

`AddSingleton*`, `AddScoped*` and `AddTransient*` follow Microsoft DI lifetimes. `TryAdd*` preserves an existing registration. Dispose scopes when their work completes, and dispose the root provider after stopping workers and flushing logs. A `UnitProduct` does not own automatic shutdown or disposal.



## UnitBase and the lifecycle

- Inherit from **UnitBase** and implement **IUnitPreparable**, **IUnitExecutable** or **IUnitPersistable**.
- Register it with `context.AddSingletonUnit<TUnit>()` (this also registers the type for instance creation).
- Instances are created by `product.Context.CreateInstances()`, and each instance is registered to the notification radio by the `UnitBase` constructor.
- Notify all the units via `product.Context.SendPrepareAsync()` and the other `Send*Async` methods.

| Interface | Method | Sent by | Description |
| --- | --- | --- | --- |
| `IUnitPreparable` | `PrepareAsync()` | `SendPrepareAsync()` | Called once at the very beginning. |
| `IUnitPersistable` | `LoadAsync()` | `SendLoadAsync()` | Called once after `PrepareAsync()`. Throw `PanicException` to abort. |
| `IUnitExecutable` | `StartAsync()` | `SendStartAsync()` | Called after `LoadAsync()`; may be called multiple times. |
| `IUnitExecutable` | `StopAsync()` | `SendStopAsync()` | Called after `StartAsync()`. |
| `IUnitPersistable` | `SaveAsync()` | `SendSaveAsync()` | May be called multiple times. |
| `IUnitExecutable` | `TerminateAsync()` | `SendTerminateAsync()` | Called once at the beginning of the termination process. |

`UnitContext` also provides `ServiceProvider`, `ExecutionRoot` (the root of the background tasks), `Options` (`UnitOptions`), `Radio`, `CommandTypes`/`SubcommandTypes` and `IsTerminationRequested`.

The table describes the intended lifecycle, not an enforced state machine. Each `Send*Async` call forwards a notification; the caller controls order, repetition and cancellation. `IsTerminationRequested` is an independent application flag: setting it does not cancel `ExecutionRoot`. Use `try/finally` around application work to ensure shutdown runs when a command throws.



## Options

Options are classes with a parameterless constructor; records make `with` updates convenient. `GetOrCreateOptions<TOptions>()` gets or creates an instance, and `SetOptions<TOptions>()` shallow-copies its private, public and inherited instance fields into the existing object. References inside an options object remain shared.

Call `GetOrCreateOptions<TOptions>()` before the provider is built, or register the options as a singleton, to make them injectable. A new options type first requested in `PostConfigure` is kept in the context but cannot be added to the already-built provider. Finish configuration before concurrent use; updates are not atomic. Log outputs may capture settings when constructed, so configure their options before resolving them.

```csharp
builder.PostConfigure(context =>
{
    context.SetOptions(context.GetOrCreateOptions<FileLogOutputOptions>() with
    {
        FilePath = Path.Combine(context.DataDirectory, "Logs/Log.txt"),
        MaxLogCapacityInMegabytes = 2,
    });
});
```

`IUnitPreConfigurationContext.GetCustomContext<TContext>()` provides a shared context (`IUnitCustomContext`) which can carry information between builders. `IUnitCustomContext.Configure()` is called after the configuration delegates of all the builders.



## Logging

Inject `ILogger<TLogSource>` (the source type is used as the category of the log), obtain a `LogWriter` for the level, and write the message. `GetWriter()` returns `null` when no output is assigned, so the message is not even created:

```csharp
this.logger.GetWriter()?.Write("Information");
this.logger.GetWriter(LogLevel.Error)?.Write($"Error: {code}");
```

Log levels are `Debug`, `Information` (default), `Warning`, `Error` and `Fatal`.
Use `ILogger<DefaultLogSource>` (or `ILogger`) to omit the source name from the formatted text.

### Resolvers

A resolver determines the output and filter for each exact source/level pair. Resolvers share the context in registration order; later assignments win. Results, including disabled levels, are cached. Resolvers must be thread-safe because concurrent cache misses can invoke them more than once.

```csharp
context.ClearLogOutputResolvers(); // Clears the default resolver (all logs -> ConsoleLogOutput).
context.AddLogOutputResolver(x =>
{// Log source/level -> Resolver() -> Output/filter
    if (x.LogLevel <= LogLevel.Debug)
    {
        x.SetOutput<ConsoleLogOutput>();
        return;
    }

    x.SetOutput<ConsoleAndFileLogOutput>();
    if (x.LogSourceType == typeof(MyCommand))
    {
        x.SetFilter<MyLogFilter>(); // The filter type must be registered in the DI container.
    }
});
```

### Outputs

| Output | Description |
| --- | --- |
| `ConsoleLogOutput` | Writes to the console via `IConsoleService`. Set `ConsoleLogOutputOptions.EnableBuffering` to write logs from a background worker. |
| `FileLogOutput<TOptions>` | Writes to a file (one file per day) from a background worker. The oldest files are deleted when the total size exceeds `FileLogOutputOptions.MaxLogCapacityInMegabytes`. |
| `ConsoleAndFileLogOutput` | Writes to both `ConsoleLogOutput` and `FileLogOutput<FileLogOutputOptions>`. |
| `MemoryLogOutput` | Keeps the formatted logs in memory (`MemoryLogOutput.ToUtf8Array()`). |
| `EmptyLogOutput` | Discards all logs. |

To add another file log output, derive a new options type from `FileLogOutputOptions`, register it (`context.TryAddSingleton<MyFileLogOutputOptions>()`), and use `FileLogOutput<MyFileLogOutputOptions>` as the output type. The open generic registration creates the log output automatically.

### Filters

A filter is applied before the log is written, and it can change the destination or discard the log.

The replacement writer supplies the output and level; the original source, event ID and log service are preserved. Its filter is not invoked again. Returning `null` or a default writer discards the event. Register outputs and filters as singletons: brokers are shared across scopes.

```csharp
public LogWriter? Filter(LogFilterContext context)
{
    if (context.LogLevel == LogLevel.Error)
    {
        return context.LogService.GetWriter<ConsoleAndFileLogOutput>(LogLevel.Fatal); // Error -> Fatal
    }

    return context.OriginalWriter; // null to discard the log.
}
```

### Flush

Buffered outputs are written by background workers. Before exiting, flush them and terminate the workers:

```csharp
var logUnit = product.Context.ServiceProvider.GetRequiredService<LogUnit>();
await logUnit.FlushAsync();             // Flushes all the buffered outputs.
await logUnit.FlushConsoleAsync();      // Flushes the console output only.
await logUnit.FlushAndTerminateAsync(); // Flushes all the buffered outputs and terminates the workers.
```

`LogUnit.SetTimestampOffset()` adjusts the timestamp of the log events, and `SimpleLogFormatterOptions` customizes the format ("Timestamp [Level Source(EventId)] Message").

`FlushAsync()` processes one batch per output (up to 1,000 console events or 10,000 file events). `FlushAndTerminateAsync()` closes worker queues, drains accepted events and rejects later writes. Stop producers before calling it. A flush failure in one output does not prevent other registered outputs from being flushed.

### Capacity and file retention

- Console and file `MaxQueueLength` limits are enforced atomically. Full queues discard new events; zero or negative values mean unlimited.
- File names use the UTC date, independently of timestamp formatting and the current culture. Relative paths are resolved when the log output is created. Give each file log output a distinct path.
- File cleanup only targets the configured prefix, a valid `yyyyMMdd` date and the configured extension. `MaxLogCapacityInMegabytes` uses decimal megabytes and periodic whole-file eviction; zero or negative values retain no files at cleanup. Files at exactly the limit are kept.
- `ClearLogsAtStartup` runs before the worker accepts events. `DeleteAllLogs()` is serialized with file writes but leaves queued events intact. File I/O is best-effort: failed batches are not retried, and flush counts describe dequeued events rather than durable writes.
- `MemoryLogOutputOptions.MaxRetainedBytes` limits retained UTF-8 bytes, not total managed memory. An oversized event evicts prior events and is discarded. `Clear()` retains reusable storage; `ToUtf8Array()` returns an independent snapshot. UTF-8 output has no ANSI colors.

`ILogService` and typed loggers are scoped. Reuse `GetLogger(Type)` results when the source is known only at runtime; each call creates a wrapper. Prefer `ILogger<T>` injection for known source types.



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

- `Build(string[] args)` consumes each element directly and preserves empty values, whitespace and literal quotes. The input array is snapshotted.
- `Build(string? args)` parses a string. `"A B"`, `'A B'`, `{A B}` and `"""A B"""` keep enclosed text together; single, double and triple quotes are removed, while braces remain part of the value.
- Option names are compared case-insensitively without their dashes. Duplicates are retained and lookup returns the first. An option without a value is stored as an empty string; a token starting with `-` starts another option.
- `|` separates tokens in string input and is retained as a value; it does not stop parsing. Array input keeps embedded pipes literal. `RawArguments` is the original string or a space-joined display of array arguments, not a reversible command-line encoding.

Two options are handled by the builder itself:

| Option | Description |
| --- | --- |
| `-ProgramDirectory` | The directory where the program is located (the default is the current directory). |
| `-DataDirectory` | The directory used for data storage (the default is empty). |

A relative path is combined with the current directory. The result is exposed as `UnitOptions` (`UnitName`, `ProgramDirectory`, `DataDirectory`).



## Commands

Command types can be registered during the configuration phase. When using [SimpleCommandLine](https://github.com/archi-Doc/SimpleCommandLine), use its generic registration extensions and create the parser from the built unit:

```csharp
using SimpleCommandLine;

context.AddCommand<ExampleCommand, ExampleCommandOptions>();
context.AddSubcommand<ExampleSubcommand>();

var parser = product.Context.CreateSimpleParser(parserOptions);
await parser.ParseAndExecute(args);
```

These methods register the command in the DI container and preserve its command and options metadata for trimming and Native AOT. The default lifetime is `Scoped`. Register every nested options type with `context.AddOptionType<TOptions>()`.

Pass a scope's `ServiceProvider` in `SimpleParserOptions` when commands need scoped lifetime and disposal. QuickStart shows this pattern. Parser instances contain mutable parse state and should not be shared across concurrent executions.

The non-generic `Arc.Unit` methods (`context.AddCommand(typeof(ExampleCommand))` and `AddSubcommand`) remain available for other parsers. `IUnitCommandContext.GetCommandGroup(Type)` gets the group identified by a type (e.g. the subcommands of a command), and `UnitContext.GetCommandTypes(Type)` returns the commands which belong to the group.



## Console service

`IConsoleService` abstracts the console input/output, so that the output can be colored, suppressed or captured.

```csharp
consoleService.WriteLine("Text", ConsoleColor.Red);
var result = await consoleService.ReadLineAsync(cancellationToken);
if (result.IsSuccess)
{
    // result.Text
}
```

`ConsoleService` is registered by default, and `EmptyConsoleService` discards all the output. `ConsoleHelper` provides the escape sequences (colors, cursor and erase operations).

`ReadLineAsync()` reports an empty line as success, EOF or I/O failure as `Terminated`, and cancellation as `Canceled`. `EmptyConsoleService` returns successful empty input unless the token is already canceled. `ReadKey()` and `KeyAvailable` suppress console errors. Color settings control emitted escape sequences, not terminal support.

`PathHelper` provides path composition, best-effort directory/file operations and a cached container check. Both byte-array and `ReadOnlyMemory<byte>` append overloads avoid copying. Try methods suppress I/O errors; append argument validation and cancellation before opening the file still throw.



## Native AOT

Arc.Unit is trim-compatible and Native AOT-compatible (`IsAotCompatible`), so an application can be published with:

```
dotnet publish -r win-x64 -p:PublishAot=true
```

The public API is annotated with `DynamicallyAccessedMembers`, so the trimmer preserves what the DI container needs. Two points to keep in mind:

- **Open generic services must be closed with reference types.** `Microsoft.Extensions.DependencyInjection` cannot create a generic service with a value type argument on Native AOT, so a log source (`ILogger<TLogSource>`) and any open generic registration of your own must be closed with a class or an interface, not with a struct.
- **Register commands and their options statically.** With SimpleCommandLine, use `AddCommand<TCommand, TOptions>()`/`AddSubcommand<TCommand, TOptions>()`, register nested options with `AddOptionType<TOptions>()`, and create the parser with `UnitContext.CreateSimpleParser()`. The runtime type-discovery overloads are not trim-safe.



## Samples

- [QuickStart](/QuickStart): a console application with commands, a log filter and a file log output.
- [Playground](/Playground): a sandbox which exercises the builder, the log outputs and the termination process.

Historical sources in `Playground/Obsolete` remain in the repository but are excluded from compilation.

## Performance and concurrency

The logging path caches brokers and delegates. Worker queues reuse storage; memory logging reuses circular UTF-8 storage instead of allocating a byte array for every event. File batches use reusable buffers without a final array copy, and daily paths are cached. Flush target snapshots change only when an output is registered. Reflection metadata for options is cached per generic type.

Warm allocation tests cover fixed-size memory log writes, queue enqueue/dequeue and empty console reads. Growing buffers, creating message strings, snapshot APIs, first-use initialization and asynchronous file I/O may still allocate. Avoid interpolating messages before checking `GetWriter()`; the null-conditional form in the logging examples skips message creation for disabled levels.

## Build and tests

```sh
dotnet build -c Release
dotnet test --project Arc.Unit.Tests/Arc.Unit.Tests.csproj -c Release --coverage --coverage-output-format cobertura --results-directory artifacts/coverage
dotnet publish QuickStart/QuickStart.csproj -c Release -r win-x64 -p:PublishAot=true -p:TreatWarningsAsErrors=true
```

Tests use xUnit v3 and Microsoft Testing Platform. They cover argument parsing, options copying, builder composition, DI lifetimes, lifecycle notifications, log routing, bounded queues, file retention, console errors and warm allocation budgets. Coverage reports are written to `artifacts/coverage`; they include generated CrossChannel code. Interactive terminal behavior and OS-specific failures still need platform testing. CI runs tests with coverage and a Linux NativeAOT command smoke test.



## License

Arc.Unit is licensed under the [MIT License](/LICENSE).
