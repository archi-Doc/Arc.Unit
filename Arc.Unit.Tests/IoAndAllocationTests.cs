using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit.Tests;

public class IoAndAllocationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FileAndConsoleOutputsFlushAllMessages(bool buffered)
    {
        var directory = Directory.CreateTempSubdirectory("arc-unit-test-");
        var console = new CaptureConsole();
        try
        {
            using var unit = new TestUnitScope(new UnitBuilder().Configure(c =>
            {
                c.Services.AddSingleton<IConsoleService>(console);
                c.ClearLogOutputResolvers();
                c.AddLogOutputResolver(r => r.SetOutput<ConsoleAndFileLogOutput>());
            }).PostConfigure(c =>
            {
                c.SetOptions(new FileLogOutputOptions { FilePath = Path.Combine(directory.FullName, "test.txt"), MaxQueueLength = 0 });
                c.SetOptions(new ConsoleLogOutputOptions { EnableBuffering = buffered, MaxQueueLength = 0, FormatterOptions = new(false) { TimestampFormat = null } });
            }));
            var service = unit.Context.ServiceProvider.GetRequiredService<ILogService>();
            var file = unit.Context.ServiceProvider.GetRequiredService<FileLogOutput<FileLogOutputOptions>>();
            var logUnit = unit.Context.ServiceProvider.GetRequiredService<LogUnit>();
            for (var i = 0; i < 1200; i++)
            {
                service.GetWriter<DefaultLogSource>()?.Write($"message-{i}");
            }

            await logUnit.FlushConsoleAsync();
            await logUnit.FlushAndTerminateAsync();
            Assert.Equal(1200, console.Lines.Count);
            Assert.Equal("[INF] message-0", console.Lines[0]);
            Assert.Equal("[INF] message-1199", console.Lines[^1]);
            var lines = await File.ReadAllLinesAsync(file.GetCurrentPath());
            Assert.Equal(1200, lines.Length);
            Assert.EndsWith("message-1199", lines[^1]);
            file.DeleteAllLogs();
            Assert.False(File.Exists(file.GetCurrentPath()));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task FileWorkerFlushesMultipleBatchesAndIgnoresLateWrites()
    {
        var directory = Directory.CreateTempSubdirectory("arc-unit-test-");
        using var unit = new TestUnitScope(new UnitBuilder());
        var worker = new FileLogOutputWorker(unit.Context.ExecutionRoot, new FileLogOutputOptions { FilePath = Path.Combine(directory.FullName, "batch.txt"), ClearLogsAtStartup = true });
        try
        {
            for (var i = 0; i < 10001; i++)
            {
                worker.Add(new(null!, typeof(DefaultLogSource), LogLevel.Information, 0, "line"));
            }

            Assert.Equal(10001, await worker.FlushAsync(true));
            worker.Add(default);
            Assert.Equal(0, worker.Count);
            Assert.Equal(10001, (await File.ReadAllLinesAsync(worker.GetCurrentPath())).Length);
        }
        finally
        {
            await worker.FlushAsync(true);
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task ConsoleInputDistinguishesEmptyLineEofCancellationAndErrors()
    {
        var original = Console.In;
        try
        {
            var service = new ConsoleService();
            Console.SetIn(new StringReader("\nhello\n"));
            Assert.True((await service.ReadLineAsync()).IsSuccess);
            Assert.Equal("hello", (await service.ReadLineAsync()).Text);
            Assert.True((await service.ReadLineAsync()).IsTerminated);
            Assert.True((await service.ReadLineAsync(new CancellationToken(true))).IsCanceled);
            Console.SetIn(new FailingReader());
            Assert.True((await service.ReadLineAsync()).IsTerminated);
        }
        finally
        {
            Console.SetIn(original);
        }
    }

    [Fact]
    public void ConsoleWritesLargeAndColoredSpans()
    {
        var original = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            var service = new ConsoleService();
            service.Write("text", ConsoleColor.Red);
            Assert.Contains("[", output.ToString());
            output.GetStringBuilder().Clear();
            service.EnableColor = false;
            service.Write((string?)null);
            service.WriteLine();
            service.WriteLine(new string('x', 20_000).AsSpan(), ConsoleColor.Red);
            Assert.Equal(Environment.NewLine + new string('x', 20_000) + Environment.NewLine, output.ToString());
            service.EnableColor = true;
            service.WriteLine(new string('x', 20_000), ConsoleColor.Green);
            Console.SetOut(new FailingWriter());
            service.WriteLine("ignored");
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Theory]
    [InlineData(ConsoleColor.Black)]
    [InlineData(ConsoleColor.DarkRed)]
    [InlineData(ConsoleColor.DarkGreen)]
    [InlineData(ConsoleColor.DarkYellow)]
    [InlineData(ConsoleColor.DarkBlue)]
    [InlineData(ConsoleColor.DarkMagenta)]
    [InlineData(ConsoleColor.DarkCyan)]
    [InlineData(ConsoleColor.Gray)]
    [InlineData(ConsoleColor.DarkGray)]
    [InlineData(ConsoleColor.Red)]
    [InlineData(ConsoleColor.Green)]
    [InlineData(ConsoleColor.Yellow)]
    [InlineData(ConsoleColor.Blue)]
    [InlineData(ConsoleColor.Magenta)]
    [InlineData(ConsoleColor.Cyan)]
    [InlineData(ConsoleColor.White)]
    public void ConsoleColorsRoundTrip(ConsoleColor expected)
    {
        var foreground = ConsoleHelper.GetForegroundColorEscapeCode(expected);
        var lastEscape = foreground.LastIndexOf('[');
        var code = int.Parse(foreground.AsSpan(lastEscape + 1, foreground.Length - lastEscape - 2), CultureInfo.InvariantCulture);
        Assert.True(ConsoleHelper.TryGetForegroundColor(code, foreground.StartsWith("[1m", StringComparison.Ordinal), out var actual));
        Assert.Equal(expected, actual);
        var background = ConsoleHelper.GetBackgroundColorEscapeCode(expected);
        code = int.Parse(background.AsSpan(2, background.Length - 3), CultureInfo.InvariantCulture);
        Assert.True(ConsoleHelper.TryGetBackgroundColor(code, out actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task PathHelpersAppendAndHandleIoFailure()
    {
        var directory = Directory.CreateTempSubdirectory("arc-unit-test-");
        try
        {
            var file = Path.Combine(directory.FullName, "bytes");
            Assert.Equal(directory.FullName, PathHelper.CombineDirectory(directory.FullName, ""));
            Assert.Equal(file, PathHelper.CombineDirectory(directory.FullName, "bytes"));
            Assert.Equal(file, PathHelper.GetRootedFile(directory.FullName, "bytes"));
            Assert.Equal(file, PathHelper.GetRootedDirectory(directory.FullName, "bytes"));
            Assert.Equal(file, PathHelper.GetRootedFile("ignored", file));
            Assert.Equal(directory.FullName, PathHelper.GetRootedDirectory("ignored", directory.FullName));
            Assert.Equal(directory.FullName, PathHelper.CombineDirectory("ignored", directory.FullName));
            Assert.True(await PathHelper.TryAppendAllBytesAsync(file, new byte[] { 1, 2 }));
            Assert.True(await PathHelper.TryAppendAllBytesAsync(file, new ReadOnlyMemory<byte>([3, 4])));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, await File.ReadAllBytesAsync(file));
            Assert.False(await PathHelper.TryAppendAllBytesAsync(directory.FullName, Array.Empty<byte>()));
            await Assert.ThrowsAsync<OperationCanceledException>(() => PathHelper.TryAppendAllBytesAsync(file, Array.Empty<byte>(), new CancellationToken(true)));
            Assert.Null(PathHelper.TryCreateDirectory(file));
            Assert.NotNull(PathHelper.TryCreateDirectory(Path.Combine(directory.FullName, "child")));
            Assert.True(PathHelper.TryDeleteDirectory(Path.Combine(directory.FullName, "child")));
            Assert.False(PathHelper.TryDeleteDirectory(Path.Combine(directory.FullName, "missing")));
            Assert.True(PathHelper.TryDeleteFile(file));
            Assert.False(PathHelper.TryDeleteFile(directory.FullName));
            Assert.Equal(PathHelper.IsRunningInContainer, PathHelper.IsRunningInContainer);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public void WarmMemoryLogOutputQueueAndEmptyConsoleDoNotAllocatePerOperation()
    {
        var memory = new MemoryLogOutput(new() { MaxRetainedBytes = 512, FormatterOptions = new(false) { TimestampFormat = null } });
        var queue = new LogEventQueue();
        var console = new EmptyConsoleService();
        var log = new LogEvent(null!, typeof(DefaultLogSource), LogLevel.Information, 0, "constant");
        for (var i = 0; i < 10_000; i++)
        {
            memory.Output(log);
            queue.Enqueue(log, 1);
            queue.TryDequeue(out _);
            _ = console.ReadLineAsync();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            memory.Output(log);
            queue.Enqueue(log, 1);
            queue.TryDequeue(out _);
            _ = console.ReadLineAsync();
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private sealed class CaptureConsole : IConsoleService
    {
        public List<string> Lines { get; } = new();
        public bool EnableColor { get; set; }
        public bool KeyAvailable => false;
        public ConsoleKeyInfo ReadKey(bool intercept) => default;
        public Task<InputResult> ReadLineAsync(CancellationToken cancellationToken = default) => Task.FromResult(new InputResult(""));
        public void Write(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor) => this.Write(message.AsSpan(), color);
        public void Write(ReadOnlySpan<char> message, ConsoleColor color = ConsoleHelper.DefaultColor) { }
        public void WriteLine(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor) => this.WriteLine(message.AsSpan(), color);
        public void WriteLine(ReadOnlySpan<char> message, ConsoleColor color = ConsoleHelper.DefaultColor) => this.Lines.Add(message.ToString());
    }

    private sealed class FailingReader : TextReader
    {
        public override string? ReadLine() => throw new IOException();
    }

    private sealed class FailingWriter : StringWriter
    {
        public override void WriteLine(ReadOnlySpan<char> value) => throw new IOException();
    }
}
