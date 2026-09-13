// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Arc.Unit;
using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;

namespace QuickStart;

public class ConsoleUnit : UnitBase, IUnitPreparable, IUnitExecutable
{
    public class Builder : UnitBuilder<Product>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {
            // Configuration of the unit
            this.PreConfigure(context =>
            {
                if (string.IsNullOrEmpty(context.ProgramDirectory))
                {
                    context.ProgramDirectory = "Program";
                }

                if (string.IsNullOrEmpty(context.DataDirectory))
                {
                    context.DataDirectory = "Test";
                }
            });

            this.Configure(context =>
            {
                context.AddSingletonUnit<ConsoleUnit>();

                // Command
                context.AddCommand<ConsoleCommand>();

                // Log filter
                context.AddSingleton<ExampleLogFilter>();

                // Log output
                context.ClearLogOutputResolvers();
                context.AddLogOutputResolver(x =>
                {// Log source/level -> Resolver() -> Output/filter
                    if (x.LogLevel <= LogLevel.Debug)
                    {
                        x.SetOutput<ConsoleLogOutput>();
                        return;
                    }

                    x.SetOutput<ConsoleAndFileLogOutput>();

                    if (x.LogSourceType == typeof(ConsoleCommand))
                    {
                        x.SetFilter<ExampleLogFilter>();
                    }
                });
            });

            this.PostConfigure(context =>
            {
                var logfile = "Logs/Log.txt";
                context.SetOptions(context.GetOrCreateOptions<FileLogOutputOptions>() with
                {
                    FilePath = Path.Combine(context.DataDirectory, logfile),
                    MaxLogCapacityInMegabytes = 2,
                    ClearLogsAtStartup = true,
                });

                var consoleLogOutputOptions = context.GetOrCreateOptions<ConsoleLogOutputOptions>();
                context.SetOptions(consoleLogOutputOptions with
                {
                    FormatterOptions = consoleLogOutputOptions.FormatterOptions with { EnableColor = true },
                });
            });
        }
    }

    public class Product : UnitProduct
    {// Product class for customizing behaviors.
        public record Param(string[] Args);

        public Product(UnitContext context)
            : base(context)
        {
        }

        public async Task RunAsync(Param param)
        {
            // Create optional instances
            this.Context.CreateInstances();

            await this.Context.SendPrepareAsync();
            await this.Context.SendStartAsync();

            await using var scope = this.Context.ServiceProvider.CreateAsyncScope();
            var parserOptions = SimpleParserOptions.Standard with
            {
                ServiceProvider = scope.ServiceProvider,
                RequireCommandName = false,
                RejectUnknownOptionNames = true,
            };

            // Main
            var parser = this.Context.CreateSimpleParser(parserOptions);
            try
            {
                await parser.ParseAndExecute(param.Args, this.Context.ExecutionRoot.CancellationToken);
            }
            finally
            {
                try
                {
                    await this.Context.SendStopAsync();
                }
                finally
                {
                    await this.Context.SendTerminateAsync();
                }
            }
        }
    }

    private class ExampleLogFilter : ILogFilter
    {
        public LogWriter? Filter(LogFilterContext context)
        {// Log source/Event id/LogLevel -> Filter() -> ILog
            if (context.LogSourceType == typeof(ConsoleCommand))
            {
                // return null; // No log
                if (context.LogLevel == LogLevel.Error)
                {
                    return context.LogService.GetWriter<ConsoleAndFileLogOutput>(LogLevel.Fatal); // Error -> Fatal
                }
                else if (context.LogLevel == LogLevel.Fatal)
                {
                    return context.LogService.GetWriter<ConsoleAndFileLogOutput>(LogLevel.Error); // Fatal -> Error
                }
            }

            return context.OriginalWriter;
        }
    }

    public ConsoleUnit(UnitContext context, ILogger<ConsoleUnit> logger, UnitOptions options)
        : base(context)
    {
        this.logger = logger;
        this.options = options;
    }

    Task IUnitPreparable.PrepareAsync(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit prepared.");
        this.logger.GetWriter()?.Write($"Program: {this.options.ProgramDirectory}");
        this.logger.GetWriter()?.Write($"Data: {this.options.DataDirectory}");
        return Task.CompletedTask;
    }

    Task IUnitExecutable.StartAsync(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit started.");
        return Task.CompletedTask;
    }

    Task IUnitExecutable.StopAsync(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit stopped.");
        return Task.CompletedTask;
    }

    Task IUnitExecutable.TerminateAsync(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit terminated.");
        return Task.CompletedTask;
    }

    private readonly ILogger<ConsoleUnit> logger;
    private readonly UnitOptions options;
}
