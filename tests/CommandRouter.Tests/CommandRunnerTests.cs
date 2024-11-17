namespace CommandRouter.Tests
{
    using System.Threading.Tasks;
    using Moq;
    using Routing;
    using Converters;
    using Xunit;
    using System;
    using System.Collections.Generic;
    using Activation;
    using Attributes;
    using CommandRouter.Binding;
    using Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Results;

    public class CommandRunnerTests
    {
        [Fact]
        public async Task RunAsync_HandlesExceptions()
        {
            var commandMethod = new CommandMethod(
                command: "test",
                action: (_, _) => Task.FromException(new TestException()),
                parameters: []
            );

            var commandTable = new Mock<ICommandTable>(MockBehavior.Strict);
            commandTable.Setup(c => c.TryGetValue("test", out commandMethod)).Returns(true);

            object[]? outExtra = null;
            var commandSelector = new Mock<ICommandSelector>(MockBehavior.Strict);
            commandSelector.Setup(c => c.TrySelectCommand("test", commandTable.Object, out commandMethod, out outExtra)).Returns(true);

            var pBinder = new ParameterBinder([]);

            var commandRunner = new CommandRunner(commandTable.Object, commandSelector.Object, pBinder);

            await Assert.ThrowsAsync<TestException>(() => commandRunner.RunAsync("test"));
        }

        [Fact]
        public async Task RunAsync_CanResolveScopedServices()
        {
            var services = new ServiceCollection();

            services.AddScoped<DisposableService>();
            services.AddTransient<TestCommand>();

            var activator = new DefaultCommandActivator(services.BuildServiceProvider());

            var commandTable = new CommandTable(activator);

            commandTable.RegisterCommands<TestCommand>();

            var runner = new CommandRunner(commandTable, new DefaultCommandSelector(),
                new ParameterBinder(new List<IPropertyConverter>()));

            // Test sync
            await runner.RunAsync("test");

            // Test await async
            await runner.RunAsync("test-async");
        }

        private class DisposableService : IDisposable
        {
            public Action? OnDispose { get; set; }

            public void Dispose()
            {
                OnDispose?.Invoke();
            }
        }

#pragma warning disable CA1822
        private class TestCommand : Command
        {
            [Command("test")]
            public void Hello()
            {
            }

            [Command("test-async")]
            public async Task<ICommandResult> HelloAsync()
            {
                await Task.Delay(1000);

                return new StringResult("hello");
            }
        }
#pragma warning restore CA1822

        private class TestException : Exception;
    }
}
