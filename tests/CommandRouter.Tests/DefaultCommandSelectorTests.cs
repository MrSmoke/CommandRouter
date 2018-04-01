namespace CommandRouter.Tests
{
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
    }
}
