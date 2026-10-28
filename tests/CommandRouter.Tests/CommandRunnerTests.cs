namespace CommandRouter.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Activation;
    using Attributes;
    using CommandRouter.Binding;
    using Commands;
    using Converters;
    using Exceptions;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Results;
    using Routing;
    using Xunit;

    public class CommandRunnerTests
    {
        [Fact]
        public async Task RunAsync_HandlesExceptions()
        {
            var commandMethod = new CommandMethod(
                command: "test",
                action: (_, _) => ValueTask.FromException<ICommandResult>(new TestException()),
                parameters: []
            );

            var commandTable = new Mock<ICommandTable>(MockBehavior.Strict);
            commandTable.Setup(c => c.TryGetValue("test", out commandMethod)).Returns(true);

            var outMatch = new CommandMatch(commandMethod, 1);
            var commandSelector = new Mock<ICommandSelector>(MockBehavior.Strict);
            commandSelector.Setup(c => c.TrySelect(It.Is<TokenizedCommand>(t => t.Tokens.Count == 1 && t.Tokens[0].Value == "test"), commandTable.Object, out outMatch)).Returns(true);

            var pBinder = new ParameterBinder([]);

            var commandRunner = new CommandRunner(commandTable.Object, new DefaultCommandTokenizer(), commandSelector.Object, pBinder);

            await Assert.ThrowsAsync<TestException>(() => commandRunner.RunAsync("test"));
        }

        [Fact]
        public async Task RunAsync_UnterminatedQuote_ThrowsInvalidTokenException()
        {
            var commandMethod = new CommandMethod(
                command: "say",
                action: (_, _) => new ValueTask<ICommandResult>(EmptyResult.Empty),
                parameters: []
            );

            var commandTable = new Mock<ICommandTable>();
            commandTable.Setup(c => c.TryGetValue("say", out commandMethod)).Returns(true);

            var commandRunner = new CommandRunner(commandTable.Object, new DefaultCommandTokenizer(), new DefaultCommandSelector(), new ParameterBinder([]));

            var ex = await Assert.ThrowsAsync<InvalidTokenException>(() => commandRunner.RunAsync("say \"hello"));

            Assert.Equal(4, ex.Position);
        }

        [Fact]
        public async Task RunAsync_UsesCustomTokenizer()
        {
            var commandMethod = new CommandMethod(
                command: "say",
                action: (_, _) => new ValueTask<ICommandResult>(EmptyResult.Empty),
                parameters: []
            );

            var commandTable = new Mock<ICommandTable>();
            commandTable.Setup(c => c.TryGetValue("say", out commandMethod)).Returns(true);

            var custom = new TokenizedCommand("say", [new CommandToken("say", 0, 3, false)]);
            var tokenizer = new Mock<ICommandTokenizer>();
            tokenizer.Setup(t => t.TryTokenize("anything", out custom, out It.Ref<int>.IsAny)).Returns(true);

            var commandRunner = new CommandRunner(commandTable.Object, tokenizer.Object, new DefaultCommandSelector(), new ParameterBinder([]));

            await commandRunner.RunAsync("anything");

            tokenizer.Verify(t => t.TryTokenize("anything", out custom, out It.Ref<int>.IsAny), Times.Once);
        }

        [Fact]
        public async Task RunAsync_UnknownCommand_ThrowsCommandNotFoundException()
        {
            var commandTable = new Mock<ICommandTable>();

            var commandRunner = new CommandRunner(commandTable.Object, new DefaultCommandTokenizer(), new DefaultCommandSelector(), new ParameterBinder([]));

            await Assert.ThrowsAsync<CommandNotFoundException>(() => commandRunner.RunAsync("nope"));
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

            var runner = new CommandRunner(commandTable, new DefaultCommandTokenizer(), new DefaultCommandSelector(),
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
