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
            var commandMethod = new CommandMethod
            {
                Action = (i, ii) => Task.FromException(new TestException()),
                ReturnType = typeof(Task)
            };

            var commandTable = new Mock<ICommandTable>();
            commandTable.Setup(c => c.TryGetValue("test", out commandMethod)).Returns(true);

            object[] outExtra = new object[0];
            var commandSelector = new Mock<ICommandSelector>();
            commandSelector.Setup(c => c.SelectCommand("test", commandTable.Object, out outExtra)).Returns(commandMethod);

            var pBinder = new CommandRouter.Binding.ParameterBinder(new List<IPropertyConverter>());

            var commandRunner = new CommandRunner(commandTable.Object, commandSelector.Object, pBinder);

            await Assert.ThrowsAsync<TestException>(async () =>
                await commandRunner.RunAsync("test").ConfigureAwait(false)).ConfigureAwait(false);
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
            public Action OnDispose { get; set; }

            public void Dispose()
            {
                OnDispose?.Invoke();
            }
        }

        private class TestCommand : Command
        {
            private readonly DisposableService _disposableService;

            public TestCommand(DisposableService disposableService)
            {
                _disposableService = disposableService;
            }

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


        private class TestException : Exception
        {

        }
    }
}
