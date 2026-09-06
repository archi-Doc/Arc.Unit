using System.Globalization;
using System.Text;
using Arc.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit.Tests;

public class RegressionTests
{
    [Fact]
    public void ArrayArgumentsPreserveLiteralValues()
    {
        string[] values = ["", "a b", "a\"b", "'quoted'", "{braces}", "a|b", "a\\", "日本語"];
        foreach (var value in values)
        {
            using var provider = (ServiceProvider)new UnitBuilder().PreConfigure(c =>
            {
                Assert.True(c.Arguments.TryGetOptionValue("value", out var actual));
                Assert.Equal(value, actual);
            }).Build(["-value", value]).Context.ServiceProvider;
        }
    }

    [Theory]
    [InlineData("-value \"a'b\" -next ok", "a'b")]
    [InlineData("-value '''' -next ok", "")]
    [InlineData("-value \"\"\"a b\"\"\" -next ok", "a b")]
    [InlineData("-value \"\" -next ok", "")]
    [InlineData("-value 'a \"\"\"b\"\"\" c' -next ok", "a \"\"\"b\"\"\" c")]
    [InlineData("-value {a \"\"\"b c\"\"\" d} -next ok", "{a \"\"\"b c\"\"\" d}")]
    public void QuotedArguments(string text, string expected)
    {
        var args = new UnitArguments(text);
        Assert.True(args.TryGetOptionValue("value", out var value));
        Assert.Equal(expected, value);
        Assert.True(args.TryGetOptionValue("next", out value));
        Assert.Equal("ok", value);
    }

    [Fact]
    public void PostConfigurationUpdatesBasicOptions()
    {
        var product = new UnitBuilder().PostConfigure(c =>
        {
            c.UnitName = "final";
            c.ProgramDirectory = "program";
            c.DataDirectory = "data";
        }).Build();
        using var provider = (ServiceProvider)product.Context.ServiceProvider;
        Assert.Equal("final", product.Context.Options.UnitName);
        Assert.Equal("program", product.Context.Options.ProgramDirectory);
        Assert.Equal("data", product.Context.Options.DataDirectory);
    }

    [Fact]
    public void OptionsCopyKeepsIdentityAndInheritedPrivateFields()
    {
        DerivedOptions? original = null;
        var product = new UnitBuilder().PreConfigure(c => original = c.GetOptions<DerivedOptions>())
            .PostConfigure(c => c.SetOptions(new DerivedOptions(42) { Name = "updated" })).Build();
        using var provider = (ServiceProvider)product.Context.ServiceProvider;
        var options = product.Context.GetOptions<DerivedOptions>();
        Assert.Same(original, options);
        Assert.Equal(42, options!.Number);
        Assert.Equal("updated", options.Name);
    }

    [Fact]
    public void DefaultWriterDiscardsMessages()
    {
        default(LogWriter).Write("discarded");
    }

    [Fact]
    public async Task EmptyConsoleHonorsCancellationAndReusesResult()
    {
        var console = new EmptyConsole();
        Assert.True((await console.ReadLine(new CancellationToken(true))).IsCanceled);
        Assert.Same(console.ReadLine(), console.ReadLine());
    }

    [Fact]
    public async Task FileCleanupPreservesUnrelatedFilesAndExactCapacity()
    {
        var directory = Directory.CreateTempSubdirectory("arc-unit-test-");
        var root = new ExecutionRoot();
        var worker = new FileLoggerWorker(root, new FileLoggerOptions { Path = Path.Combine(directory.FullName, "Log.txt"), MaxLogCapacity = 1 });
        try
        {
            var unrelated = Path.Combine(directory.FullName, "Logabcdefgh.txt");
            var invalidDate = Path.Combine(directory.FullName, "Log20260230.txt");
            var log = Path.Combine(directory.FullName, "Log20260101.txt");
            await File.WriteAllTextAsync(unrelated, "keep");
            await File.WriteAllTextAsync(invalidDate, "keep");
            await File.WriteAllBytesAsync(log, new byte[1_000_000]);
            worker.LimitLogs(false);
            Assert.True(File.Exists(log));
            worker.LimitLogs(true);
            Assert.False(File.Exists(log));
            Assert.True(File.Exists(unrelated));
            Assert.True(File.Exists(invalidDate));
        }
        finally
        {
            root.RequestTermination();
            await worker.Flush(true);
            await root.WaitForTermination(TerminationOptions.IncludeIndependent).WaitAsync(TimeSpan.FromSeconds(10));
            directory.Delete(true);
        }
    }

    public class BaseOptions
    {
        private readonly int number;
        public BaseOptions(int number) => this.number = number;
        public int Number => this.number;
    }

    public class DerivedOptions : BaseOptions
    {
        public DerivedOptions() : base(0) { }
        public DerivedOptions(int number) : base(number) { }
        public string Name { get; init; } = "";
    }
}
