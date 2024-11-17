namespace CommandRouter.Tests
{
    using System.Collections.Generic;
    using CommandRouter.Binding;
    using Routing;
    using Moq;
    using Xunit;

    public class DefaultCommandSelectorTests
    {
        [Fact]
        public void SelectCommand_EmptyString_ReturnsMethod()
        {
            var commandMethod = new CommandMethod();

            var commandTable = new Mock<ICommandTable>();
            commandTable.Setup(c => c.TryGetValue("", out commandMethod)).Returns(true);
            commandTable.Setup(c => c.TryGetValue("test", out commandMethod)).Returns(true);

            var commandSelector = new DefaultCommandSelector();

            var selectedMethod = commandSelector.SelectCommand("", commandTable.Object, out object[] extra);

            Assert.NotNull(selectedMethod);
            Assert.Equal(selectedMethod.Id, commandMethod.Id);
            Assert.Empty(extra);
        }

        [Fact]
        public void SelectCommand_RootRouteWithParameter_ReturnsMethod()
        {
            var commandMethod = new CommandMethod
            {
                Parameters = new List<ParameterInfo> {
                    new ParameterInfo
                    {
                        HasDefaultValue = false,
                        Name = "param1",
                        Type = typeof(string)
                    }
                }
            };

            var commandTable = new Mock<ICommandTable>();
            commandTable.Setup(c => c.TryGetValue("", out commandMethod)).Returns(true);

            var commandSelector = new DefaultCommandSelector();

            var selectedMethod = commandSelector.SelectCommand("bacon", commandTable.Object, out object[] extra);

            Assert.NotNull(selectedMethod);
            Assert.Equal(selectedMethod.Id, commandMethod.Id);
            Assert.Single(extra);
            Assert.Equal("bacon", extra[0]);
        }
    }
}
