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

                // Logger
                context.ClearLoggerResolver();
                context.AddLoggerResolver(x =>
                {// Log source/level -> Resolver() -> Output/filter
                    if (x.LogLevel <= LogLevel.Debug)
                    {
                        x.SetOutput<ConsoleLogger>();
                        return;
                    }

                    x.SetOutput<ConsoleAndFileLogger>();

                    if (x.LogSourceType == typeof(ConsoleCommand))
                    {
                        x.SetFilter<ExampleLogFilter>();
                    }
                });
            });

            this.PostConfigure(context =>
            {
                var logfile = "Logs/Log.txt";
                context.SetOptions(context.GetOptions<FileLoggerOptions>() with
                {
                    Path = Path.Combine(context.DataDirectory, logfile),
                    MaxLogCapacity = 2,
                    ClearLogsAtStartup = true,
                });

                var consoleLoggerOptions = context.GetOptions<ConsoleLoggerOptions>();
                context.SetOptions(consoleLoggerOptions with
                {
                    FormatterOptions = consoleLoggerOptions.FormatterOptions with { EnableColor = true },
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

            await this.Context.SendPrepare();
            await this.Context.SendStart();

            await using var scope = this.Context.ServiceProvider.CreateAsyncScope();
            var parserOptions = SimpleParserOptions.Standard with
            {
                ServiceProvider = scope.ServiceProvider,
                RequireStrictCommandName = false,
                RequireStrictOptionName = true,
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
                    await this.Context.SendStop();
                }
                finally
                {
                    await this.Context.SendTerminate();
                }
            }
        }
    }

    private class ExampleLogFilter : ILogFilter
    {
        public LogWriter? Filter(LogFilterParameter parameter)
        {// Log source/Event id/LogLevel -> Filter() -> ILog
            if (parameter.LogSourceType == typeof(ConsoleCommand))
            {
                // return null; // No log
                if (parameter.LogLevel == LogLevel.Error)
                {
                    return parameter.LogService.GetWriter<ConsoleAndFileLogger>(LogLevel.Fatal); // Error -> Fatal
                }
                else if (parameter.LogLevel == LogLevel.Fatal)
                {
                    return parameter.LogService.GetWriter<ConsoleAndFileLogger>(LogLevel.Error); // Fatal -> Error
                }
            }

            return parameter.OriginalWriter;
        }
    }

    public ConsoleUnit(UnitContext context, ILogger<ConsoleUnit> logger, UnitOptions options)
        : base(context)
    {
        this.logger = logger;
        this.options = options;
    }

    Task IUnitPreparable.Prepare(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit prepared.");
        this.logger.GetWriter()?.Write($"Program: {this.options.ProgramDirectory}");
        this.logger.GetWriter()?.Write($"Data: {this.options.DataDirectory}");
        return Task.CompletedTask;
    }

    Task IUnitExecutable.Start(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit started.");
        return Task.CompletedTask;
    }

    Task IUnitExecutable.Stop(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit stopped.");
        return Task.CompletedTask;
    }

    Task IUnitExecutable.Terminate(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit terminated.");
        return Task.CompletedTask;
    }

    private readonly ILogger<ConsoleUnit> logger;
    private readonly UnitOptions options;
}
