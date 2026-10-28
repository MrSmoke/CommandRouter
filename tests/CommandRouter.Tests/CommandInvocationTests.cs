namespace CommandRouter.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Activation;
    using Attributes;
    using CommandRouter.Binding;
    using Commands;
    using Converters;
    using Exceptions;
    using Microsoft.Extensions.DependencyInjection;
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
        public async Task RunAsync_CommandThrows_StillDisposesScope()
        {
            var probe = new ScopeProbe();
            var runner = CreateRunner<ScopeCommand>(s => s.AddSingleton(probe).AddScoped<ScopedService>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync("scope-throw"));

            Assert.True(probe.Disposed);
        }

        [Fact]
        public async Task RunAsync_CommandNotRegisteredInContainer_ThrowsCommandRouterException()
        {
            var table = new CommandTable(new DefaultCommandActivator(new ServiceCollection().BuildServiceProvider()));
            table.RegisterCommands<ReturnTypeCommand>();
            var runner = CreateRunner(table);

            var ex = await Assert.ThrowsAsync<CommandRouterException>(() => runner.RunAsync("return-void"));

            Assert.Contains(nameof(ReturnTypeCommand), ex.Message);
        }

        [Fact]
        public void RegisterCommands_UnsupportedReturnType_ThrowsCommandRouterException()
        {
            var table = new CommandTable(new DefaultCommandActivator(new ServiceCollection().BuildServiceProvider()));

            var ex = Assert.Throws<CommandRouterException>(() => table.RegisterCommands<UnsupportedReturnCommand>());

            Assert.Contains("bad-return", ex.Message);
        }

        [Fact]
        public void RegisterCommands_DuplicateCommand_ThrowsCommandRouterException()
        {
            var table = new CommandTable(new DefaultCommandActivator(new ServiceCollection().BuildServiceProvider()));

            var ex = Assert.Throws<CommandRouterException>(() => table.RegisterCommands<DuplicateCommand>());

            Assert.Contains("dupe", ex.Message);
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
                new ParameterBinder([new SystemTypeConverter()]));
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

        private class ScopedService : IDisposable
        {
            private readonly ScopeProbe _probe;

            public ScopedService(ScopeProbe probe)
            {
                _probe = probe;
            }

            public void Dispose()
            {
                _probe.Disposed = true;
            }
        }

#pragma warning disable CA1822, CA1859
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

        private class ScopeCommand : Command
        {
            private readonly ScopeProbe _probe;

            public ScopeCommand(ScopeProbe probe, ScopedService service)
            {
                _probe = probe;
                Service = service;
            }

            public ScopedService Service { get; }

            [Command("scope-sync")]
            public void Sync()
            {
                _probe.DisposedWhileRunning = _probe.Disposed;
            }

            [Command("scope-task")]
            public async Task TaskScope()
            {
                await Task.Yield();
                _probe.DisposedWhileRunning = _probe.Disposed;
            }

            [Command("scope-valuetask")]
            public async ValueTask ValueTaskScope()
            {
                await Task.Yield();
                _probe.DisposedWhileRunning = _probe.Disposed;
            }

            [Command("scope-throw")]
            public void Throw() => throw new InvalidOperationException();
        }

        private class UnsupportedReturnCommand : Command
        {
            [Command("bad-return")]
            public int Bad() => 1;
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
}
