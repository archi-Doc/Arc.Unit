// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Arc.Unit;
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
                context.AddSingleton<ConsoleUnit>();
                context.RegisterInstanceCreation<ConsoleUnit>();

                // Command
                context.AddCommand(typeof(ConsoleCommand));

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

            var parserOptions = SimpleParserOptions.Standard with
            {
                ServiceProvider = this.Context.ServiceProvider,
                RequireStrictCommandName = false,
                RequireStrictOptionName = true,
            };

            // Main
            // await SimpleParser.ParseAndRunAsync(this.Context.Commands, "example -string test", parserOptions);
            await SimpleParser.ParseAndRunAsync(this.Context.Commands, param.Args, parserOptions);

            await this.Context.SendStop();
            await this.Context.SendTerminate();
        }
    }

    private class ExampleLogFilter : ILogFilter
    {
        public ExampleLogFilter(ConsoleUnit consoleUnit)
        {
            this.consoleUnit = consoleUnit;
        }

        public LogWriter? Filter(LogFilterParameter param)
        {// Log source/Event id/LogLevel -> Filter() -> ILog
            if (param.LogSourceType == typeof(ConsoleCommand))
            {
                // return null; // No log
                if (param.LogLevel == LogLevel.Error)
                {
                    return param.LogService.GetWriter<ConsoleAndFileLogger>(LogLevel.Fatal); // Error -> Fatal
                }
                else if (param.LogLevel == LogLevel.Fatal)
                {
                    return param.LogService.GetWriter<ConsoleAndFileLogger>(LogLevel.Error); // Fatal -> Error
                }
            }

            return param.OriginalLogger;
        }

        private ConsoleUnit consoleUnit;
    }

    public ConsoleUnit(UnitContext context, ILogger<ConsoleUnit> logger, UnitOptions options)
        : base(context)
    {
        this.logger = logger;
        this.options = options;
    }

    async Task IUnitPreparable.Prepare(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit prepared.");
        this.logger.GetWriter()?.Write($"Program: {this.options.ProgramDirectory}");
        this.logger.GetWriter()?.Write($"Data: {this.options.DataDirectory}");
    }

    async Task IUnitExecutable.Start(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit started.");
    }

    async Task IUnitExecutable.Stop(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit stopped.");
    }

    async Task IUnitExecutable.Terminate(UnitContext unitContext, CancellationToken cancellationToken)
    {
        this.logger.GetWriter()?.Write("Unit terminated.");
    }

    private ILogger<ConsoleUnit> logger;
    private UnitOptions options;
}
