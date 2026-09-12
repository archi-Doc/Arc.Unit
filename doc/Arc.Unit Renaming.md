# Arc.Unit Renaming

Public API names were revised after Arc.Unit 0.47.0. Behavior is unchanged; only names changed.
This document lists every rename so that dependent projects can migrate with find/replace.

## Quick migration tips

- Replace whole words (word boundaries), except the `*Logger` → `*LogOutput` rule below, which is intentionally a substring rule.
- `ConsoleLogger` → `ConsoleLogOutput`, `FileLogger` → `FileLogOutput`, `MemoryLogger` → `MemoryLogOutput` and `EmptyLogger` → `EmptyLogOutput` also fix `*LoggerOptions`, `IFileLogger` and `ConsoleAndFileLogger`. Check that your own types with similar names (e.g. `MyFileLogger`) are not renamed unintentionally.
- `ILogger`, `ILogger<T>`, `ILogService`, `LogUnit`, `LogWriter` and `SimpleLogFormatter` are **not** renamed.
- Implementations of interfaces and overrides must be renamed too (see [Implementers](#implementers)).
- Rename the members below only on the listed types (e.g. `GetOptions` is renamed on the configuration contexts, but not on `UnitContext`).

## Types

| Old | New |
| --- | --- |
| `IUnitConfigurationAndPostConfigurationContext` | `IUnitCommandContext` |
| `IUnitSerializable` | `IUnitPersistable` |
| `DefaultLog` | `DefaultLogSource` |
| `LogFilterParameter` | `LogFilterContext` |
| `LoggerResolverDelegate` | `LogOutputResolver` |
| `LoggerResolverContext` | `LogOutputResolverContext` |
| `EmptyLogger` | `EmptyLogOutput` |
| `ConsoleLogger` | `ConsoleLogOutput` |
| `ConsoleLoggerOptions` | `ConsoleLogOutputOptions` |
| `FileLogger<TOption>` | `FileLogOutput<TOptions>` |
| `IFileLogger` | `IFileLogOutput` |
| `FileLoggerOptions` | `FileLogOutputOptions` |
| `MemoryLogger` | `MemoryLogOutput` |
| `MemoryLoggerOptions` | `MemoryLogOutputOptions` |
| `ConsoleAndFileLogger` | `ConsoleAndFileLogOutput` |
| `UnitBuilderToServiceProviderFactory` | `UnitServiceProviderFactory` |
| `EmptyConsole` | `EmptyConsoleService` |

## Members

### Configuration contexts (`IUnitPreConfigurationContext`, `IUnitConfigurationContext`, `IUnitPostConfigurationContext`)

| Old | New |
| --- | --- |
| `GetOptions<TOptions>()` | `GetOrCreateOptions<TOptions>()` |
| `AddLoggerResolver(LoggerResolverDelegate)` | `AddLogOutputResolver(LogOutputResolver)` |
| `ClearLoggerResolver()` | `ClearLogOutputResolvers()` |
| `GetCommandGroup()` (no argument) | `GetTopLevelCommandGroup()` |

`GetCommandGroup(Type)` and `GetSubcommandGroup()` keep their names.

### `IUnitCustomContext`

| Old | New |
| --- | --- |
| `ProcessContext(IUnitConfigurationContext)` | `Configure(IUnitConfigurationContext)` |

### `UnitContext`

| Old | New |
| --- | --- |
| `Commands` | `CommandTypes` |
| `Subcommands` | `SubcommandTypes` |
| `CommandDictionary` | `CommandTypesByGroup` |
| `LoggerResolvers` | `LogOutputResolvers` |
| `TerminationRequested` | `IsTerminationRequested` |
| `SendPrepare()` | `SendPrepareAsync()` |
| `SendLoad()` | `SendLoadAsync()` |
| `SendStart()` | `SendStartAsync()` |
| `SendStop()` | `SendStopAsync()` |
| `SendSave()` | `SendSaveAsync()` |
| `SendTerminate()` | `SendTerminateAsync()` |

`UnitContext.GetOptions<TOptions>()` keeps its name (it returns `null` if the options are not found).

### Unit notification interfaces

| Old | New |
| --- | --- |
| `IUnitPreparable.Prepare()` | `IUnitPreparable.PrepareAsync()` |
| `IUnitExecutable.Start()` | `IUnitExecutable.StartAsync()` |
| `IUnitExecutable.Stop()` | `IUnitExecutable.StopAsync()` |
| `IUnitExecutable.Terminate()` | `IUnitExecutable.TerminateAsync()` |
| `IUnitSerializable.Load()` | `IUnitPersistable.LoadAsync()` |
| `IUnitSerializable.Save()` | `IUnitPersistable.SaveAsync()` |

### Logging

| Old | New |
| --- | --- |
| `LogUnit.Flush()` | `LogUnit.FlushAsync()` |
| `LogUnit.FlushConsole()` | `LogUnit.FlushConsoleAsync()` |
| `LogUnit.FlushAndTerminate()` | `LogUnit.FlushAndTerminateAsync()` |
| `LogUnit.SetTimeOffset(TimeSpan)` | `LogUnit.SetTimestampOffset(TimeSpan)` |
| `LogUnit.GroupName` | `LogUnit.WorkerGroupName` |
| `LogUnit.IsGroupIndependent` | `LogUnit.IsWorkerGroupIndependent` |
| `BufferedLogOutput.Flush(bool)` | `BufferedLogOutput.FlushAsync(bool)` |
| `IFileLogger.Flush(bool)` | `IFileLogOutput.FlushAsync(bool)` |
| `LoggerResolverContext.SetOutputType(Type)` | `LogOutputResolverContext.SetOutput(Type)` |
| `LoggerResolverContext.SetFilterType(Type)` | `LogOutputResolverContext.SetFilter(Type)` |
| `ConsoleLoggerOptions.MaxQueue` | `ConsoleLogOutputOptions.MaxQueueLength` |
| `ConsoleLoggerOptions.DefaultMaxQueue` | `ConsoleLogOutputOptions.DefaultMaxQueueLength` |
| `FileLoggerOptions.MaxQueue` | `FileLogOutputOptions.MaxQueueLength` |
| `FileLoggerOptions.DefaultMaxQueue` | `FileLogOutputOptions.DefaultMaxQueueLength` |
| `FileLoggerOptions.Path` | `FileLogOutputOptions.FilePath` |
| `FileLoggerOptions.DefaultPath` | `FileLogOutputOptions.DefaultFilePath` |
| `FileLoggerOptions.MaxLogCapacity` | `FileLogOutputOptions.MaxLogCapacityInMegabytes` |
| `MemoryLoggerOptions.MaxMemoryUsage` | `MemoryLogOutputOptions.MaxRetainedBytes` |
| `MemoryLoggerOptions.DefaultMaxMemoryUsage` | `MemoryLogOutputOptions.DefaultMaxRetainedBytes` |
| `SimpleLogFormatterOptions.TimestampLocal` | `SimpleLogFormatterOptions.UseLocalTimestamp` |

### Console and helpers

| Old | New |
| --- | --- |
| `IConsoleService.ReadLine()` | `IConsoleService.ReadLineAsync()` |
| `InputResultKind.IsPositive` (extension) | `InputResultKind.IsSuccess` |
| `InputResultKind.IsNegative` (extension) | `InputResultKind.IsNo` |
| `ConsoleHelper.ResetSpan` | `ConsoleHelper.ResetAttributesSpan` |
| `ConsoleHelper.SetCursorSpan` | `ConsoleHelper.CursorPositionPrefixSpan` |
| `ConsoleHelper.ResetCursorSpan` | `ConsoleHelper.MoveCursorHomeSpan` |
| `PathHelper.TryAppendAllBytes()` | `PathHelper.TryAppendAllBytesAsync()` |
| `PathHelper.RunningInContainer` | `PathHelper.IsRunningInContainer` |

## Parameter names

These only affect named arguments (`Method(name: value)`).

| Member | Old | New |
| --- | --- | --- |
| `UnitBuilder.PreConfigure/Configure/PostConfigure` | `delegate` | `configureDelegate` |
| `UnitArguments.ContainsOption` | `option` | `optionName` |
| `IUnitCommandContext.GetCommandGroup` | `type` | `groupType` |
| `UnitContext.GetCommandTypes` | `commandType` | `groupType` |
| `ILogService.GetLogger(Type)` | `logSource` | `logSourceType` |
| `ILogFilter.Filter` | `parameter` | `context` |
| `LogUnit.SetTimestampOffset` | `timeSpan` | `offset` |
| `SimpleLogFormatter.Format(StringBuilder, LogEvent)` | `sb` | `builder` |
| `PathHelper.GetRootedDirectory/GetRootedFile` | `rootDirectory` | `baseDirectory` |
| `UnitServiceProviderFactory.CreateServiceProvider` | `builder` | `containerBuilder` |
| `ConsoleAndFileLogOutput` constructor | `consoleLogger`, `fileLogger` | `consoleLogOutput`, `fileLogOutput` |

## Implementers

Update these signatures in your classes; the old names no longer satisfy the interface or override.

```csharp
// Units
Task IUnitPreparable.PrepareAsync(UnitContext unitContext, CancellationToken cancellationToken);
Task IUnitExecutable.StartAsync(UnitContext unitContext, CancellationToken cancellationToken);
Task IUnitExecutable.StopAsync(UnitContext unitContext, CancellationToken cancellationToken);
Task IUnitExecutable.TerminateAsync(UnitContext unitContext, CancellationToken cancellationToken);
Task IUnitPersistable.LoadAsync(UnitContext unitContext, CancellationToken cancellationToken);
Task IUnitPersistable.SaveAsync(UnitContext unitContext, CancellationToken cancellationToken);

// Custom context
void IUnitCustomContext.Configure(IUnitConfigurationContext context);

// Log filter / output
LogWriter? ILogFilter.Filter(LogFilterContext context);
public override Task<int> FlushAsync(bool terminate); // BufferedLogOutput

// Console
Task<InputResult> IConsoleService.ReadLineAsync(CancellationToken cancellationToken = default);
```

## Before / after

```csharp
// Before
context.ClearLoggerResolver();
context.AddLoggerResolver(x => x.SetOutput<ConsoleAndFileLogger>());
context.SetOptions(context.GetOptions<FileLoggerOptions>() with { Path = "Logs/Log.txt", MaxLogCapacity = 2 });
await product.Context.SendPrepare();
await logUnit.FlushAndTerminate();

// After
context.ClearLogOutputResolvers();
context.AddLogOutputResolver(x => x.SetOutput<ConsoleAndFileLogOutput>());
context.SetOptions(context.GetOrCreateOptions<FileLogOutputOptions>() with { FilePath = "Logs/Log.txt", MaxLogCapacityInMegabytes = 2 });
await product.Context.SendPrepareAsync();
await logUnit.FlushAndTerminateAsync();
```

## Libraries compiled against the old names

Binaries built against Arc.Unit 0.47.0 or earlier fail at runtime (`TypeLoadException` / `MissingMethodException`) when used with the renamed Arc.Unit. Rebuild and re-release them.

SimpleCommandLine (0.46.0) uses the following, and must be updated before applications (e.g. QuickStart) run with the new Arc.Unit:

| SimpleCommandLine usage | Change |
| --- | --- |
| `SimpleCommandConfiguration : IUnitCustomContext` / `ProcessContext()` | Rename to `Configure()` |
| `context.GetCommandGroup()` | `context.GetTopLevelCommandGroup()` |
| `UnitContext.Commands` | `UnitContext.CommandTypes` |
| `UnitContext.Subcommands` | `UnitContext.SubcommandTypes` |
