namespace CommandRouter.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Activation;
using Attributes;
using Commands;
using Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Results;
using Routing;
using Xunit;

public class CommandInvocationTests
{
    [Theory]
    [InlineData("return-result", "result")]
    [InlineData("return-task-result", "task-result")]
    [InlineData("return-task-derived", "task-derived")]
    [InlineData("return-valuetask-result", "valuetask-result")]
    [InlineData("return-valuetask-derived", "valuetask-derived")]
    public async Task RunAsync_ReturnsCommandResult(string command, string expected)
    {
        var runner = CreateRunner<ReturnTypeCommand>();

        var result = await runner.RunAsync(command);

        Assert.Equal(expected, await ReadAsync(result));
    }

    [Theory]
    [InlineData("return-void")]
    [InlineData("return-task")]
    [InlineData("return-valuetask")]
    [InlineData("return-null")]
    public async Task RunAsync_NoResult_ReturnsEmptyResult(string command)
    {
        var runner = CreateRunner<ReturnTypeCommand>();

        var result = await runner.RunAsync(command);

        Assert.Same(EmptyResult.Empty, result);
    }

    [Fact]
    public async Task RunAsync_PassesBoundParametersToCommand()
    {
        var runner = CreateRunner<ReturnTypeCommand>();

        var result = await runner.RunAsync("add 1 2");

        Assert.Equal("3", await ReadAsync(result));
    }

    [Theory]
    [InlineData("throw-sync")]
    [InlineData("throw-task")]
    [InlineData("throw-valuetask")]
    public async Task RunAsync_CommandThrows_ThrowsOriginalException(string command)
    {
        var runner = CreateRunner<ThrowingCommand>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(command));
    }

    [Theory]
    [InlineData("scope-sync")]
    [InlineData("scope-task")]
    [InlineData("scope-valuetask")]
    public async Task RunAsync_DisposesScopeOnlyAfterCommandCompletes(string command)
    {
        var probe = new ScopeProbe();
        var runner = CreateRunner<ScopeCommand>(s => s.AddSingleton(probe).AddScoped<ScopedService>());

        await runner.RunAsync(command);

        Assert.False(probe.DisposedWhileRunning);
        Assert.True(probe.Disposed);
    }

    [Fact]
    public async Task RunAsync_ScopedServiceOnlyAsyncDisposable_DisposesAsync()
    {
        var probe = new ScopeProbe();
        var runner = CreateRunner<AsyncScopeCommand>(s => s.AddSingleton(probe).AddScoped<AsyncOnlyScopedService>());

        await runner.RunAsync("scope-async-only");

        Assert.True(probe.Disposed);
    }

    [Fact]
    public async Task RunAsync_CommandThrows_StillDisposesScope()
    {
        var probe = new ScopeProbe();
        var runner = CreateRunner<ScopeCommand>(s => s.AddSingleton(probe).AddScoped<ScopedService>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync("scope-throw"));

        Assert.True(probe.Disposed);
    }

    [Fact]
    public async Task RunAsync_CommandNotRegisteredInContainer_IsCreatedWithItsDependencies()
    {
        var probe = new ScopeProbe();
        var services = new ServiceCollection().AddSingleton(probe).AddScoped<ScopedService>();
        var table = new CommandTable(new DefaultCommandActivator(services.BuildServiceProvider()));
        table.RegisterCommands<ScopeCommand>();

        await CreateRunner(table).RunAsync("scope-sync");

        Assert.False(probe.DisposedWhileRunning);
        Assert.True(probe.Disposed);
    }

    [Fact]
    public async Task RunAsync_CommandRegisteredInContainer_UsesRegistration()
    {
        var created = 0;
        var services = new ServiceCollection().AddTransient(_ =>
        {
            created++;
            return new ReturnTypeCommand();
        });
        var table = new CommandTable(new DefaultCommandActivator(services.BuildServiceProvider()));
        table.RegisterCommands<ReturnTypeCommand>();

        await CreateRunner(table).RunAsync("return-void");

        Assert.Equal(1, created);
    }

    [Fact]
    public async Task RunAsync_ActivatorReturnsNonCommand_ThrowsCommandRouterException()
    {
        var scope = new Mock<ICommandActivatorScope>();
        scope.Setup(s => s.CommandActivator.Create(It.IsAny<Type>())).Returns(new object());
        var activator = new Mock<ICommandActivator>();
        activator.Setup(a => a.CreateScope()).Returns(scope.Object);

        var table = new CommandTable(activator.Object);
        table.RegisterCommands<ReturnTypeCommand>();

        var ex = await Assert.ThrowsAsync<CommandRouterException>(() => CreateRunner(table).RunAsync("return-void"));

        Assert.Contains(nameof(ReturnTypeCommand), ex.Message);
    }

    [Fact]
    public void RegisterCommands_UnsupportedReturnType_ThrowsUnsupportedReturnTypeException()
    {
        var table = new CommandTable(new DefaultCommandActivator(new ServiceCollection().BuildServiceProvider()));

        var ex = Assert.Throws<UnsupportedReturnTypeException>(() => table.RegisterCommands<UnsupportedReturnCommand>());

        Assert.Equal("bad-return", ex.Command);
        Assert.Equal(typeof(int), ex.ReturnType);
    }

    [Fact]
    public void RegisterCommands_CommandWithExtraSpaces_IsNormalised()
    {
        var table = new CommandTable(Mock.Of<ICommandActivator>());

        table.RegisterCommands<ExtraSpacesCommand>();

        Assert.Equal(["user add"], table.Keys);
    }

    [Fact]
    public async Task RunAsync_ContextItems_AreOnCommandContext()
    {
        var probe = new ContextProbe();
        var runner = CreateRunner<ContextCommand>(s => s.AddSingleton(probe));
        IReadOnlyDictionary<string, object> items = new Dictionary<string, object> { ["key"] = "value" };

        await runner.RunAsync("context", items);

        Assert.Equal("value", probe.Context!.Items["key"]);
    }

    [Fact]
    public async Task RunAsync_CancellationToken_IsOnCommandContext()
    {
        var probe = new ContextProbe();
        var runner = CreateRunner<ContextCommand>(s => s.AddSingleton(probe));
        using var cts = new CancellationTokenSource();

        await runner.RunAsync("context", cts.Token);

        Assert.Equal(cts.Token, probe.Context!.CancellationToken);
    }

    [Fact]
    public async Task RunAsync_AlreadyCancelled_ThrowsWithoutRunningCommand()
    {
        var probe = new ContextProbe();
        var runner = CreateRunner<ContextCommand>(s => s.AddSingleton(probe));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runner.RunAsync("context", new CancellationToken(true)));

        Assert.Null(probe.Context);
    }

    [Fact]
    public async Task RunAsync_InheritedCommand_UsesDerivedPrefixAndInstance()
    {
        var runner = CreateRunner<AdminCommand>();

        var result = await runner.RunAsync("admin status");

        Assert.Equal("status:" + nameof(AdminCommand), await ReadAsync(result));
    }

    [Fact]
    public async Task RunAsync_OverrideWithoutAttribute_KeepsBaseCommandName()
    {
        var runner = CreateRunner<OverridingCommand>();

        var result = await runner.RunAsync("ping");

        Assert.Equal("derived", await ReadAsync(result));
    }

    [Fact]
    public void RegisterCommands_DerivedClassesWithDifferentPrefixes_ShareBaseCommands()
    {
        var table = new CommandTable(Mock.Of<ICommandActivator>());

        table.RegisterCommands<AdminCommand>();
        table.RegisterCommands<OpsCommand>();

        Assert.Equal(["admin status", "ops status"], table.Keys.Order());
    }

    [Fact]
    public void RegisterCommands_DuplicateCommand_ThrowsCommandRouterException()
    {
        var table = new CommandTable(new DefaultCommandActivator(new ServiceCollection().BuildServiceProvider()));

        var ex = Assert.Throws<CommandRouterException>(() => table.RegisterCommands<DuplicateCommand>());

        Assert.Contains("dupe", ex.Message);
    }

    [Fact]
    public void RegisterCommands_CommandWithNoNameOrPrefix_RegistersRootCommand()
    {
        var table = new CommandTable(Mock.Of<ICommandActivator>());

        table.RegisterCommands<NoNameCommand>();

        Assert.Equal([""], table.Keys);
    }

    [Fact]
    public void RegisterCommands_EmptyCommandName_RegistersRootCommand()
    {
        var table = new CommandTable(Mock.Of<ICommandActivator>());

        table.RegisterCommands<RootCommand>();

        Assert.True(table.ContainsKey(""));
    }

    [Fact]
    public void RegisterCommands_CommandWithNoNameAndPrefix_RegistersPrefix()
    {
        var table = new CommandTable(Mock.Of<ICommandActivator>());

        table.RegisterCommands<PrefixOnlyCommand>();

        Assert.Equal(["admin", "user"], table.Keys.Order());
    }

    private static CommandRunner CreateRunner<T>(Action<IServiceCollection>? configure = null) where T : Command
    {
        var services = new ServiceCollection();
        services.AddTransient<T>();
        configure?.Invoke(services);

        var table = new CommandTable(new DefaultCommandActivator(services.BuildServiceProvider()));
        table.RegisterCommands<T>();

        return CreateRunner(table);
    }

    private static CommandRunner CreateRunner(ICommandTable table)
    {
        return new CommandRunner(table, new DefaultCommandTokenizer(), new DefaultCommandSelector(),
            TestBinder.Create());
    }

    private static async Task<string> ReadAsync(ICommandResult result)
    {
        using var stream = new MemoryStream();
        await result.ExecuteAsync(stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private class ScopeProbe
    {
        public bool Disposed { get; set; }
        public bool? DisposedWhileRunning { get; set; }
    }

    private class ScopedService(ScopeProbe probe) : IDisposable
    {
        public void Dispose()
        {
            probe.Disposed = true;
        }
    }

    private sealed class AsyncOnlyScopedService(ScopeProbe probe) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            probe.Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

#pragma warning disable CA1822, CA1859
    private class AsyncScopeCommand(AsyncOnlyScopedService service) : Command
    {
        public AsyncOnlyScopedService Service { get; } = service;

        [Command("scope-async-only")]
        public void Run()
        {
        }
    }

    private class ReturnTypeCommand : Command
    {
        [Command("return-result")]
        public ICommandResult Result() => new StringResult("result");

        [Command("return-task-result")]
        public async Task<ICommandResult> TaskResult()
        {
            await Task.Yield();
            return new StringResult("task-result");
        }

        [Command("return-task-derived")]
        public async Task<StringResult> TaskDerived()
        {
            await Task.Yield();
            return new StringResult("task-derived");
        }

        [Command("return-valuetask-result")]
        public async ValueTask<ICommandResult> ValueTaskResult()
        {
            await Task.Yield();
            return new StringResult("valuetask-result");
        }

        [Command("return-valuetask-derived")]
        public async ValueTask<StringResult> ValueTaskDerived()
        {
            await Task.Yield();
            return new StringResult("valuetask-derived");
        }

        [Command("return-void")]
        public void Void()
        {
        }

        [Command("return-task")]
        public async Task TaskOnly()
        {
            await Task.Yield();
        }

        [Command("return-valuetask")]
        public async ValueTask ValueTaskOnly()
        {
            await Task.Yield();
        }

        [Command("return-null")]
        public ICommandResult? Null() => null;

        [Command("add")]
        public ICommandResult Add(int a, int b) => new StringResult((a + b).ToString());
    }

    private class ThrowingCommand : Command
    {
        [Command("throw-sync")]
        public void Sync() => throw new InvalidOperationException();

        [Command("throw-task")]
        public async Task TaskThrow()
        {
            await Task.Yield();
            throw new InvalidOperationException();
        }

        [Command("throw-valuetask")]
        public async ValueTask ValueTaskThrow()
        {
            await Task.Yield();
            throw new InvalidOperationException();
        }
    }

    private class ScopeCommand(ScopeProbe probe, ScopedService service) : Command
    {
        public ScopedService Service { get; } = service;

        [Command("scope-sync")]
        public void Sync()
        {
            probe.DisposedWhileRunning = probe.Disposed;
        }

        [Command("scope-task")]
        public async Task TaskScope()
        {
            await Task.Yield();
            probe.DisposedWhileRunning = probe.Disposed;
        }

        [Command("scope-valuetask")]
        public async ValueTask ValueTaskScope()
        {
            await Task.Yield();
            probe.DisposedWhileRunning = probe.Disposed;
        }

        [Command("scope-throw")]
        public void Throw() => throw new InvalidOperationException();
    }

    private class UnsupportedReturnCommand : Command
    {
        [Command("bad-return")]
        public int Bad() => 1;
    }

    private class NoNameCommand : Command
    {
        [Command]
        public void Unnamed()
        {
        }
    }

    private class RootCommand : Command
    {
        [Command("")]
        public void Root()
        {
        }
    }

    [CommandPrefix("user")]
    [CommandPrefix("admin")]
    private class PrefixOnlyCommand : Command
    {
        [Command]
        public void Run()
        {
        }
    }

    [CommandPrefix(" user ")]
    private class ExtraSpacesCommand : Command
    {
        [Command("  add ")]
        public void Add()
        {
        }
    }

    private class ContextProbe
    {
        public CommandContext? Context { get; set; }
    }

    private class ContextCommand(ContextProbe probe) : Command
    {
        [Command("context")]
        public void Run()
        {
            probe.Context = Context;
        }
    }

    private abstract class StatusCommandBase : Command
    {
        [Command("status")]
        public StringResult Status() => new("status:" + GetType().Name);
    }

    [CommandPrefix("admin")]
    private class AdminCommand : StatusCommandBase;

    [CommandPrefix("ops")]
    private class OpsCommand : StatusCommandBase;

    private abstract class PingCommandBase : Command
    {
        [Command("ping")]
        public virtual StringResult Ping() => new("base");
    }

    private class OverridingCommand : PingCommandBase
    {
        public override StringResult Ping() => new("derived");
    }

    private class DuplicateCommand : Command
    {
        [Command("dupe")]
        public void One()
        {
        }

        [Command("dupe")]
        public void Two()
        {
        }
    }
#pragma warning restore CA1822, CA1859
}