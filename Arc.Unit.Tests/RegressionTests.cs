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
        var product = new UnitBuilder().PreConfigure(c => original = c.GetOrCreateOptions<DerivedOptions>())
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
        var console = new EmptyConsoleService();
        Assert.True((await console.ReadLineAsync(new CancellationToken(true))).IsCanceled);
        Assert.Same(console.ReadLineAsync(), console.ReadLineAsync());
    }

    [Fact]
    public async Task FileCleanupPreservesUnrelatedFilesAndExactCapacity()
    {
        var directory = Directory.CreateTempSubdirectory("arc-unit-test-");
        var root = new ExecutionRoot();
        var worker = new FileLogOutputWorker(root, new FileLogOutputOptions { FilePath = Path.Combine(directory.FullName, "Log.txt"), MaxLogCapacityInMegabytes = 1 });
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
            await worker.FlushAsync(true);
            await root.WaitForTerminationAsync(TerminationOptions.IncludeIndependent).WaitAsync(TimeSpan.FromSeconds(10));
            directory.Delete(true);
        }
    }

    [Fact]
    public void GroupRegistrationOrderDoesNotChangeCommandLifetime()
    {
        using var unit = new TestUnitScope(new UnitBuilder().Configure(c =>
        {
            c.GetCommandGroup(typeof(ParentCommand)).AddCommand(typeof(ChildCommand));
            c.AddCommand(typeof(ParentCommand)); // Scoped, although the group was created first.
        }));
        using var first = unit.Context.ServiceProvider.CreateScope();
        using var second = unit.Context.ServiceProvider.CreateScope();
        Assert.Same(first.ServiceProvider.GetRequiredService<ParentCommand>(), first.ServiceProvider.GetRequiredService<ParentCommand>());
        Assert.NotSame(first.ServiceProvider.GetRequiredService<ParentCommand>(), second.ServiceProvider.GetRequiredService<ParentCommand>());
        Assert.Equal(new[] { typeof(ChildCommand) }, unit.Context.GetCommandTypes(typeof(ParentCommand)));
    }

    [Fact]
    public async Task FailedFileOutputConstructionDoesNotBreakFlush()
    {
        using var unit = new TestUnitScope(new UnitBuilder().PostConfigure(c => c.SetOptions(new FileLogOutputOptions { FilePath = "" })));
        Assert.Throws<ArgumentException>(() => unit.Context.ServiceProvider.GetRequiredService<FileLogOutput<FileLogOutputOptions>>());
        await unit.Context.ServiceProvider.GetRequiredService<LogUnit>().FlushAsync(); // Dispose() also runs a terminating flush.
    }

    [Fact]
    public void BuildFailureIsNotReplacedByCleanupFailure()
    {
        var builder = new UnitBuilder().Configure(c => c.AddSingleton<AsyncOnlyService>()).PostConfigure(c =>
        {
            c.ServiceProvider.GetRequiredService<AsyncOnlyService>(); // ServiceProvider.Dispose() throws for this service.
            throw new ApplicationException("original");
        });
        Assert.Equal("original", Assert.Throws<ApplicationException>(() => builder.Build()).Message);
    }

    [Fact]
    public void UnbuiltContextHasNoCommands()
    {
        var context = new UnitContext();
        Assert.Empty(context.CommandTypes);
        Assert.Empty(context.SubcommandTypes);
    }

    public class ParentCommand { }
    public class ChildCommand { }
    public sealed class AsyncOnlyService : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
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
