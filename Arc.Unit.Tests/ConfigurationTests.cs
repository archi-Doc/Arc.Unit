using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit.Tests;

public class ConfigurationTests
{
    [Fact]
    public void FailedPostConfigurationDisposesCreatedServices()
    {
        DisposableService? service = null;
        var builder = new UnitBuilder().Configure(c => c.AddSingleton<DisposableService>())
            .PostConfigure(c =>
            {
                service = c.ServiceProvider.GetRequiredService<DisposableService>();
                throw new InvalidOperationException("test failure");
            });
        Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.True(service!.Disposed);
    }

    public sealed class DisposableService : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => this.Disposed = true;
    }

    [Fact]
    public void ReentrantBuildIsRejectedAndFailureCanBeRetried()
    {
        var builder = new UnitBuilder();
        var retry = false;
        builder.Configure(_ =>
        {
            if (!retry)
            {
                builder.Build();
            }
        });
        Assert.Throws<InvalidOperationException>(() => builder.Build());
        retry = true;
        using var unit = new TestUnitScope(builder);
    }

    [Fact]
    public void CustomProductAndCustomContextAreShared()
    {
        var builder = new UnitBuilder<CustomProduct>().AddBuilder(new UnitBuilder())
            .PreConfigure(c => c.GetCustomContext<CustomContext>().Calls++)
            .Configure(c => c.GetCustomContext<CustomContext>().Calls++)
            .PostConfigure(c => Assert.Equal(3, c.GetCustomContext<CustomContext>().Calls));
        using var unit = new TestUnitScope(builder);
        Assert.IsType<CustomProduct>(unit.Product);
        Assert.Same(unit.Product, builder.GetBuiltProduct());
        Assert.NotNull(unit.Context.GetOptions<Registered>());
        Assert.Null(unit.Context.GetOptions<Unregistered>());
    }

    [Fact]
    public void ProviderFactoryImportsHostServices()
    {
        var factory = new UnitBuilderToServiceProviderFactory(new UnitBuilder());
        var services = new ServiceCollection();
        services.AddSingleton<Registered>();
        var builder = factory.CreateBuilder(services);
        using var provider = (ServiceProvider)factory.CreateServiceProvider(builder);
        Assert.NotNull(provider.GetRequiredService<Registered>());
    }

    [Fact]
    public void RegistrationHelpersRespectLifetimesAndTryAdd()
    {
        using var unit = new TestUnitScope(new UnitBuilder().Configure(c =>
        {
            c.AddSingleton<Registered>();
            c.TryAddSingleton<Registered>();
            c.AddScoped<Scoped>();
            c.TryAddScoped<Scoped>();
            c.AddTransient<Transient>();
            c.TryAddTransient<Transient>();
            c.AddSingleton(typeof(Registered));
            c.TryAddSingleton(typeof(Registered));
            c.AddScoped(typeof(Scoped));
            c.TryAddScoped(typeof(Scoped));
            c.AddTransient(typeof(Transient));
            c.TryAddTransient(typeof(Transient));
            c.AddSingleton<IRegistered, Registered>();
            c.TryAddSingleton<IRegistered, Registered>();
            c.AddScoped<IScoped, Scoped>();
            c.TryAddScoped<IScoped, Scoped>();
            c.AddTransient<ITransient, Transient>();
            c.TryAddTransient<ITransient, Transient>();
        }));
        using var a = unit.Context.ServiceProvider.CreateScope();
        using var b = unit.Context.ServiceProvider.CreateScope();
        Assert.Same(a.ServiceProvider.GetRequiredService<IRegistered>(), b.ServiceProvider.GetRequiredService<IRegistered>());
        Assert.Same(a.ServiceProvider.GetRequiredService<IScoped>(), a.ServiceProvider.GetRequiredService<IScoped>());
        Assert.NotSame(a.ServiceProvider.GetRequiredService<IScoped>(), b.ServiceProvider.GetRequiredService<IScoped>());
        Assert.NotSame(a.ServiceProvider.GetRequiredService<ITransient>(), a.ServiceProvider.GetRequiredService<ITransient>());
    }

    [Fact]
    public void ResolverUpdatesOnlyRequestedFields()
    {
        var resolver = new LoggerResolverContext(new(typeof(Registered), LogLevel.Error));
        resolver.TrySetOutput<EmptyLogger>();
        resolver.TrySetOutput<MemoryLogger>();
        Assert.Equal(typeof(EmptyLogger), resolver.LogOutputType);
        resolver.TrySetFilter<BehaviorTests.RedirectFilter>();
        resolver.TrySetOutputAndFilter<MemoryLogger, BehaviorTests.RedirectFilter>();
        Assert.Equal(typeof(EmptyLogger), resolver.LogOutputType);
        resolver.ClearOutput();
        resolver.SetOutputType(typeof(MemoryLogger));
        resolver.ClearFilter();
        resolver.SetFilterType(typeof(BehaviorTests.RedirectFilter));
        Assert.Equal(typeof(BehaviorTests.RedirectFilter), resolver.LogFilterType);
        resolver.SetFilter<BehaviorTests.RedirectFilter>();
        resolver.ClearOutputAndFilter();
        Assert.Null(resolver.LogOutputType);
        Assert.Null(resolver.LogFilterType);
        Assert.Throws<ArgumentException>(() => resolver.SetOutputType(typeof(string)));
        Assert.Throws<ArgumentException>(() => resolver.SetFilterType(typeof(string)));
        new EmptyLogger().Output(default);
    }

    [Fact]
    public void InputResultsAndDefaultColorsAreConsistent()
    {
        Assert.True(new InputResult(InputResultKind.No).IsNo);
        Assert.True(new InputResult(InputResultKind.Canceled).IsCanceled);
        Assert.True(new InputResult(InputResultKind.Terminated).IsTerminated);
        Assert.Equal("No", new InputResult(InputResultKind.No).ToString());
        Assert.Equal("input", new InputResult("input").ToString());
        Assert.Equal("", default(InputResult).Text);
        Assert.True(InputResultKind.No.IsNegative);
        Assert.True(InputResultKind.Success.IsPositive);
        Assert.True(InputResultKind.Canceled.IsCanceled);
        Assert.True(InputResultKind.Terminated.IsTerminated);
        Assert.True(ConsoleHelper.TryGetForegroundColor(39, false, out var color));
        Assert.Null(color);
        Assert.False(ConsoleHelper.TryGetForegroundColor(-1, false, out _));
        Assert.True(ConsoleHelper.TryGetBackgroundColor(49, out color));
        Assert.Null(color);
        Assert.False(ConsoleHelper.TryGetBackgroundColor(-1, out _));
        Assert.Equal(ConsoleHelper.DefaultForegroundColorEscapeCode, ConsoleHelper.GetForegroundColorEscapeCode(ConsoleHelper.DefaultColor));
        Assert.Equal(ConsoleHelper.DefaultBackgroundColorEscapeCode, ConsoleHelper.GetBackgroundColorEscapeCode(ConsoleHelper.DefaultColor));
        var empty = new EmptyConsole();
        empty.Write("discard");
        empty.Write("discard".AsSpan());
        empty.WriteLine("discard");
        empty.WriteLine("discard".AsSpan());
        Assert.False(empty.KeyAvailable);
        Assert.Equal(default, empty.ReadKey(true));
    }

    [Fact]
    public void LogValueEqualityIgnoresTimestamp()
    {
        var first = new LogEvent(null!, typeof(Registered), LogLevel.Debug, 1, "message");
        var second = new LogEvent(null!, typeof(Registered), LogLevel.Debug, 1, "message");
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.False(first.Equals("other"));
        var pair = new LogSourceLevelPair(typeof(Registered), LogLevel.Debug);
        Assert.True(pair.Equals((object)new LogSourceLevelPair(typeof(Registered), LogLevel.Debug)));
        Assert.False(pair.Equals(null));
        var parameter = new LogFilterParameter(null!, typeof(Registered), LogLevel.Debug, 1, default);
        Assert.True(parameter.Equals((object)new LogFilterParameter(null!, typeof(Registered), LogLevel.Debug, 1, default)));
        Assert.Equal(parameter.GetHashCode(), new LogFilterParameter(null!, typeof(Registered), LogLevel.Debug, 1, default).GetHashCode());
        Assert.False(parameter.Equals(null));
        Assert.Equal(typeof(EmptyLogger), default(LogWriter).OutputType);
    }

    public class CustomProduct(UnitContext context) : UnitProduct(context) { }
    public class CustomContext : IUnitCustomContext
    {
        public int Calls { get; set; }
        public void ProcessContext(IUnitConfigurationContext context)
        {
            this.Calls++;
            context.AddSingleton<Registered>();
        }
    }
    public interface IRegistered { }
    public interface IScoped { }
    public interface ITransient { }
    public class Registered : IRegistered { }
    public class Scoped : IScoped { }
    public class Transient : ITransient { }
    public class Unregistered { }
}
