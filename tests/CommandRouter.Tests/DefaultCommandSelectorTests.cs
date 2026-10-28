namespace CommandRouter.Tests
{
    using System.Threading.Tasks;
    using CommandRouter.Binding;
    using Exceptions;
    using Routing;
    using Moq;
    using Xunit;

    public class DefaultCommandSelectorTests
    {
        [Fact]
        public void SelectCommand_EmptyString_ReturnsMethod()
        {
            var commandMethod = new CommandMethod(
                command: "",
                (_, _) => Task.CompletedTask,
                parameters: []
            );

            var commandTable = new Mock<ICommandTable>();
            commandTable.Setup(c => c.TryGetValue("", out commandMethod)).Returns(true);
            commandTable.Setup(c => c.TryGetValue("test", out commandMethod)).Returns(true);

            var commandSelector = new DefaultCommandSelector();

            var result = commandSelector.TrySelectCommand("", commandTable.Object, out var selectedMethod, out var extra);

            Assert.True(result);
            Assert.NotNull(selectedMethod);
            Assert.Equal(selectedMethod.Id, commandMethod.Id);
            Assert.NotNull(extra);
            Assert.Empty(extra);
        }

        [Fact]
        public void SelectCommand_RootRouteWithParameter_ReturnsMethod()
        {
            var commandMethod = new CommandMethod(
                "test",
                (_, _) => Task.CompletedTask,
                [
                    new ParameterInfo
                    {
                        HasDefaultValue = false,
                        Name = "param1",
                        Type = typeof(string)
                    }
                ]);

            var commandTable = new Mock<ICommandTable>();
            commandTable.Setup(c => c.TryGetValue("", out commandMethod)).Returns(true);

            var commandSelector = new DefaultCommandSelector();

            var result = commandSelector.TrySelectCommand("bacon", commandTable.Object, out var selectedMethod, out var extra);

            Assert.True(result);
            Assert.NotNull(selectedMethod);
            Assert.Equal(selectedMethod.Id, commandMethod.Id);
            Assert.NotNull(extra);
            var extra1 = Assert.Single(extra);
            Assert.Equal("bacon", extra1);
        }

        [Theory]
        [InlineData("say \"hello", 4)]
        [InlineData("say \"hello world", 4)]
        [InlineData("say \"", 4)]
        [InlineData("say \"hello\" \"world", 12)]
        public void SelectCommand_UnterminatedQuote_ThrowsInvalidTokenException(string input, int expectedPosition)
        {
            var commandMethod = new CommandMethod(
                "say",
                (_, _) => Task.CompletedTask,
                [
                    new ParameterInfo
                    {
                        HasDefaultValue = false,
                        Name = "param1",
                        Type = typeof(string)
                    }
                ]);

            var commandTable = new Mock<ICommandTable>();
            commandTable.Setup(c => c.TryGetValue("say", out commandMethod)).Returns(true);

            var commandSelector = new DefaultCommandSelector();

            var ex = Assert.Throws<InvalidTokenException>(() =>
                commandSelector.TrySelectCommand(input, commandTable.Object, out _, out _));

            Assert.Equal(expectedPosition, ex.Position);
        }

        [Fact]
        public void SelectCommand_TerminatedQuote_ReturnsQuotedArgument()
        {
            var commandMethod = new CommandMethod(
                "say",
                (_, _) => Task.CompletedTask,
                [
                    new ParameterInfo
                    {
                        HasDefaultValue = false,
                        Name = "param1",
                        Type = typeof(string)
                    }
                ]);

            var commandTable = new Mock<ICommandTable>();
            commandTable.Setup(c => c.TryGetValue("say", out commandMethod)).Returns(true);

            var commandSelector = new DefaultCommandSelector();

            var result = commandSelector.TrySelectCommand("say \"hello world\"", commandTable.Object, out _, out var extra);

            Assert.True(result);
            Assert.NotNull(extra);
            Assert.Equal("hello world", Assert.Single(extra));
        }
    }
}
