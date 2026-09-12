using System.Text;
using Arc.Threading;
using Microsoft.Extensions.DependencyInjection;

[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace Arc.Unit.Tests;

public class BehaviorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyArguments(string? text)
    {
        var arguments = new UnitArguments(text);
        Assert.Empty(arguments.GetOptions());
        Assert.Empty(arguments.GetValues());
        Assert.False(arguments.TryGetOptionValue("missing", out var missing));
        Assert.Null(missing);
        Assert.Equal(text ?? "", arguments.RawArguments);
    }

    [Theory]
    [InlineData("one -FLAG -key value -key second end", "value")]
    [InlineData("one -FLAG -key 'value' -key second end", "value")]
    [InlineData("one -FLAG -key {a {b c}} -key second end", "{a {b c}}")]
    [InlineData("one -FLAG -key \"\"\"a 'b' c\"\"\" -key second end", "a 'b' c")]
    public void ArgumentsKeepOrderAndFirstOption(string text, string expected)
    {
        var args = new UnitArguments(text);
        Assert.True(args.ContainsOption("flag"));
        Assert.True(args.TryGetOptionValue("KEY", out var value));
        Assert.Equal(expected, value);
        Assert.True(args.ContainsValue("one"));
        Assert.Equal(new[] { "one", "end" }, args.GetValues());
        Assert.Equal(new[] { ("FLAG", ""), ("key", expected), ("key", "second") }, args.GetOptions());
    }

    [Fact]
    public void ArrayArgumentsAreSnapshottedAndKeepEmptyPositionals()
    {
        string[] input = ["", "'literal'", "a|b", "-flag"];
        var args = new UnitArguments(input);
        input[1] = "changed";
        Assert.Equal(new[] { "", "'literal'", "a|b" }, args.GetValues());
        Assert.Equal(" 'literal' a|b -flag", args.RawArguments);
        Assert.True(args.ContainsOption("flag"));
    }

    [Fact]
    public void BuilderProcessesCyclesOnceAndPreservesOrder()
    {
        var calls = new List<string>();
        var child = new UnitBuilder().PreConfigure(_ => calls.Add("child-pre"))
            .Configure(_ => calls.Add("child-config")).PostConfigure(_ => calls.Add("child-post"));
        var parent = new UnitBuilder().AddBuilder(child).AddBuilder(child)
            .PreConfigure(_ => calls.Add("parent-pre")).Configure(_ => calls.Add("parent-config"))
            .PostConfigure(_ => calls.Add("parent-post"));
        child.AddBuilder(parent);
        using var unit = new TestUnitScope(parent);
        Assert.Equal(new[] { "child-pre", "parent-pre", "child-config", "parent-config", "child-post", "parent-post" }, calls);
        Assert.Same(unit.Product, parent.GetBuiltProduct());
        Assert.Throws<InvalidOperationException>(() => parent.Build());
        Assert.Throws<InvalidOperationException>(() => new UnitBuilder().GetBuiltProduct());
    }

    [Fact]
    public void BuilderRejectsNullConfiguration()
    {
        var builder = new UnitBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.AddBuilder(null!));
        Assert.Throws<ArgumentNullException>(() => builder.Configure(null!));
        Assert.Throws<ArgumentNullException>(() => builder.PreConfigure(null!));
        Assert.Throws<ArgumentNullException>(() => builder.PostConfigure(null!));
        Assert.Throws<ArgumentNullException>(() => builder.SetServiceProviderFactory(null!));
    }

    [Fact]
    public async Task LifecycleSendsEveryNotificationAndCreatesOnlyOneSingleton()
    {
        using var unit = new TestUnitScope(new UnitBuilder().Configure(c => c.AddSingletonUnit<Receiver>()));
        unit.Context.CreateInstances();
        unit.Context.CreateInstances();
        await unit.Context.SendPrepare();
        await unit.Context.SendLoad();
        await unit.Context.SendStart();
        await unit.Context.SendSave();
        await unit.Context.SendStop();
        await unit.Context.SendTerminate();
        var receiver = unit.Context.ServiceProvider.GetRequiredService<Receiver>();
        Assert.Equal(new[] { "prepare", "load", "start", "save", "stop", "terminate" }, receiver.Calls);
    }

    [Fact]
    public void CommandsAreDeduplicatedAndScoped()
    {
        using var unit = new TestUnitScope(new UnitBuilder().Configure(c =>
        {
            Assert.True(c.AddCommand(typeof(Command)));
            Assert.False(c.AddCommand(typeof(Command)));
            Assert.True(c.AddSubcommand(typeof(ChildCommand)));
            c.GetCommandGroup(typeof(Command)).AddCommand(typeof(ChildCommand));
        }));
        Assert.Equal(new[] { typeof(Command) }, unit.Context.Commands);
        Assert.Equal(new[] { typeof(ChildCommand) }, unit.Context.Subcommands);
        Assert.Equal(new[] { typeof(ChildCommand) }, unit.Context.GetCommandTypes(typeof(Command)));
        Assert.Empty(unit.Context.GetCommandTypes(typeof(string)));
        using var first = unit.Context.ServiceProvider.CreateScope();
        using var second = unit.Context.ServiceProvider.CreateScope();
        Assert.Same(first.ServiceProvider.GetRequiredService<Command>(), first.ServiceProvider.GetRequiredService<Command>());
        Assert.NotSame(first.ServiceProvider.GetRequiredService<Command>(), second.ServiceProvider.GetRequiredService<Command>());
    }

    [Fact]
    public void TypedAndRuntimeLoggersPreserveSourceAndFilterLevel()
    {
        using var unit = new TestUnitScope(new UnitBuilder().Configure(c =>
        {
            c.AddSingleton<CaptureOutput>();
            c.AddSingleton<RedirectFilter>();
            c.ClearLoggerResolver();
            c.AddLoggerResolver(r =>
            {
                if (r.LogLevel != LogLevel.Debug)
                {
                    r.SetOutputAndFilter<CaptureOutput, RedirectFilter>();
                }
            });
        }));
        var service = unit.Context.ServiceProvider.GetRequiredService<ILogService>();
        Assert.Null(service.GetWriter<Command>(LogLevel.Debug));
        service.GetLogger<Command>().GetWriter(LogLevel.Error)?.Write("typed", 42);
        service.GetLogger(typeof(Command)).GetWriter()?.Write("runtime");
        service.GetWriter<Command>(LogLevel.Warning)?.Write("discarded");
        var logs = unit.Context.ServiceProvider.GetRequiredService<CaptureOutput>().Events;
        Assert.Equal(2, logs.Count);
        Assert.Equal(typeof(Command), logs[0].LogSourceType);
        Assert.Equal(LogLevel.Fatal, logs[0].LogLevel);
        Assert.Equal(42, logs[0].EventId);
        Assert.Equal("runtime", logs[1].Message);
    }

    [Theory]
    [InlineData(LogLevel.Debug, "DBG")]
    [InlineData(LogLevel.Information, "INF")]
    [InlineData(LogLevel.Warning, "WRN")]
    [InlineData(LogLevel.Error, "ERR")]
    [InlineData(LogLevel.Fatal, "FTL")]
    public void FormatterUtf8MatchesText(LogLevel level, string label)
    {
        var formatter = new SimpleLogFormatter(new(false) { TimestampFormat = null });
        var log = new LogEvent(null!, typeof(Command), level, 42, "日本語 🌏");
        Assert.Equal($"[{label} Command(002A)] 日本語 🌏", formatter.Format(log));
        Assert.Equal(formatter.Format(log) + Environment.NewLine, Encoding.UTF8.GetString(formatter.FormatUtf8(log)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    [InlineData(300)]
    public void MemoryLoggerMatchesEvictionModel(int limit)
    {
        var options = new MemoryLoggerOptions { MaxMemoryUsage = limit, FormatterOptions = new(false) { TimestampFormat = null } };
        var logger = new MemoryLogger(options);
        var formatter = new SimpleLogFormatter(options.FormatterOptions);
        var expected = new Queue<byte[]>();
        for (var i = 0; i < 100; i++)
        {
            var log = new LogEvent(null!, typeof(DefaultLog), LogLevel.Information, 0, new string('x', i % 31));
            expected.Enqueue(formatter.FormatUtf8(log));
            while (limit > 0 && expected.Sum(x => x.Length) > limit)
            {
                expected.Dequeue();
            }

            logger.Output(log);
            Assert.Equal(expected.SelectMany(x => x).ToArray(), logger.ToUtf8Array());
        }

        logger.Clear();
        Assert.Same(Array.Empty<byte>(), logger.ToUtf8Array());
    }

    [Fact]
    public void QueueCapacityIsAtomicAndCompletionRejectsWrites()
    {
        var queue = new LogEventQueue();
        Parallel.For(0, 10_000, i => queue.Enqueue(new(null!, typeof(Command), LogLevel.Information, i, "test"), 17));
        Assert.Equal(17, queue.Count);
        queue.Complete();
        var ids = new HashSet<long>();
        while (queue.TryDequeue(out var log))
        {
            Assert.True(ids.Add(log.EventId));
        }

        queue.Enqueue(default, 0);
        Assert.Equal(0, queue.Count);
        Assert.Equal(17, ids.Count);
    }

    [Fact]
    public async Task FlushContinuesAfterSynchronousFailure()
    {
        using var unit = new TestUnitScope(new UnitBuilder());
        var logUnit = unit.Context.ServiceProvider.GetRequiredService<LogUnit>();
        var broken = new FlushOutput(logUnit) { Fail = true };
        var good = new FlushOutput(logUnit);
        Assert.False(logUnit.RegisterFlushTarget(good));
        await Assert.ThrowsAsync<IOException>(() => logUnit.Flush());
        Assert.Equal(1, good.Calls);
        broken.Fail = false;
    }

    public class Command { }
    public class ChildCommand { }
    public class CaptureOutput : ILogOutput
    {
        public List<LogEvent> Events { get; } = new();
        public void Output(LogEvent logEvent) => this.Events.Add(logEvent);
    }

    public class RedirectFilter : ILogFilter
    {
        public LogWriter? Filter(LogFilterParameter parameter) => parameter.LogLevel switch
        {
            LogLevel.Warning => null,
            LogLevel.Error => parameter.LogService.GetWriter<DefaultLog>(LogLevel.Fatal),
            _ => parameter.OriginalWriter,
        };
    }

    public class FlushOutput(LogUnit logUnit) : BufferedLogOutput(logUnit)
    {
        public bool Fail { get; set; }
        public int Calls { get; private set; }
        public override Task<int> Flush(bool terminate)
        {
            this.Calls++;
            return this.Fail ? throw new IOException("test") : Task.FromResult(0);
        }
    }

    public class Receiver(UnitContext context) : UnitBase(context), IUnitPreparable, IUnitExecutable, IUnitSerializable
    {
        public List<string> Calls { get; } = new();
        private Task Record(string name) { this.Calls.Add(name); return Task.CompletedTask; }
        public Task Prepare(UnitContext context, CancellationToken token) => this.Record("prepare");
        public Task Load(UnitContext context, CancellationToken token) => this.Record("load");
        public Task Start(UnitContext context, CancellationToken token) => this.Record("start");
        public Task Save(UnitContext context, CancellationToken token) => this.Record("save");
        public Task Stop(UnitContext context, CancellationToken token) => this.Record("stop");
        public Task Terminate(UnitContext context, CancellationToken token) => this.Record("terminate");
    }
}

internal sealed class TestUnitScope : IDisposable
{
    internal TestUnitScope(UnitBuilder builder) => this.Product = builder.Build();
    internal UnitProduct Product { get; }
    internal UnitContext Context => this.Product.Context;
    public void Dispose()
    {
        this.Context.ExecutionRoot.RequestTermination();
        this.Context.ServiceProvider.GetRequiredService<LogUnit>().FlushAndTerminate().GetAwaiter().GetResult();
        this.Context.ExecutionRoot.WaitForTerminationAsync(TerminationOptions.IncludeIndependent).WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        ((IDisposable)this.Context.ServiceProvider).Dispose();
    }
}
