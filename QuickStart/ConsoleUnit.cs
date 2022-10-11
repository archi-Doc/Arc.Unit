// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Arc.Unit;
using SimpleCommandLine;

namespace QuickStart;

public class ConsoleUnit : UnitBase, IUnitPreparable, IUnitExecutable
{
    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {
            // Configuration for Unit.
            this.Configure(context =>
            {
                context.AddSingleton<ConsoleUnit>();
                context.CreateInstance<ConsoleUnit>();

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

            this.SetupOptions<FileLoggerOptions>((context, options) =>
            {// FileLoggerOptions
                var logfile = "Logs/Log.txt";
                options.Path = Path.Combine(context.RootDirectory, logfile);
                options.MaxLogCapacity = 2;
            });

            this.SetupOptions<ConsoleLoggerOptions>((context, options) =>
            {// ConsoleLoggerOptions
                options.Formatter.EnableColor = true;
            });
        }
    }

    public class Unit : BuiltUnit
    {// Unit class for customizing behaviors.
        public record Param(string[] Args);

        public Unit(UnitContext context)
            : base(context)
        {
        }

        public async Task RunAsync(Param param)
        {
            // Create optional instances
            this.Context.CreateInstances();

            this.Context.SendPrepare(new());
            await this.Context.SendRunAsync(new(ThreadCore.Root));

            var parserOptions = SimpleParserOptions.Standard with
            {
                ServiceProvider = this.Context.ServiceProvider,
                RequireStrictCommandName = false,
                RequireStrictOptionName = true,
            };

            // Main
            // await SimpleParser.ParseAndRunAsync(this.Context.Commands, "example -string test", parserOptions);
            await SimpleParser.ParseAndRunAsync(this.Context.Commands, param.Args, parserOptions);

            await this.Context.SendTerminateAsync(new());
        }
    }

    private class ExampleLogFilter : ILogFilter
    {
        public ExampleLogFilter(ConsoleUnit consoleUnit)
        {
            this.consoleUnit = consoleUnit;
        }

        public ILog? Filter(LogFilterParameter param)
        {// Log source/Event id/LogLevel -> Filter() -> ILog
            if (param.LogSourceType == typeof(ConsoleCommand) &&
                param.LogLevel == LogLevel.Error)
            {
                return param.Context.TryGet<ConsoleAndFileLogger>(LogLevel.Fatal); // Error -> Fatal
                // return null; // No log
            }

            return param.OriginalLogger;
        }

        private ConsoleUnit consoleUnit;
    }

    public ConsoleUnit(UnitContext context, ILogger<ConsoleUnit> logger)
        : base(context)
    {
        this.logger = logger;
    }

    public void Prepare(UnitMessage.Prepare message)
    {
        this.logger.TryGet()?.Log("Unit prepared.");
    }

    public async Task RunAsync(UnitMessage.RunAsync message)
    {
        this.logger.TryGet()?.Log("Unit running.");
    }

    public async Task TerminateAsync(UnitMessage.TerminateAsync message)
    {
        this.logger.TryGet()?.Log("Unit terminated.");
    }

    private ILogger<ConsoleUnit> logger;
}
