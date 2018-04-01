namespace CommandRouter.Tests
{
    using System.Threading.Tasks;
    using Moq;
    using Routing;
    using Converters;
    using Xunit;
    using System;
    using System.Collections.Generic;

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

        private class TestException : Exception
        {

        }
    }
}
