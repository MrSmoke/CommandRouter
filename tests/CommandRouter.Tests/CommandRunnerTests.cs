namespace CommandRouter.Tests
{
    using System.Threading.Tasks;
    using Moq;
    using CommandRouter.Routing;
    using CommandRouter.Converters;
    using Xunit;
    using System;
    using System.Collections.Generic;

    public class CommandRunnerTests
    {
        [Fact]
        public async Task RunAsync_HandlesExceptions()
        {
            CommandMethod commandMethod = new CommandMethod
            {
                Action = (i, ii) => {
                    return Task.FromException(new TestException());
                },
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
                await commandRunner.RunAsync("test"));

        }

        private class TestException : Exception
        {

        }
    }
}
