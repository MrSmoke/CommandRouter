namespace CommandRouter.Tests;

using System.Threading.Tasks;
using Activation;
using CommandRouter.Binding;
using Moq;
using Results;
using Routing;
using Xunit;

public class DefaultCommandSelectorTests
{
    private static TokenizedCommand Tokenize(string str)
    {
        Assert.True(new DefaultCommandTokenizer().TryTokenize(str, out var command, out _));
        return command;
    }

    private static CommandTable CreateTable(params string[] commands)
    {
        var table = new CommandTable(Mock.Of<ICommandActivator>());

        foreach (var command in commands)
            table.AddCommand(command, (_, _) => EmptyResult.Empty);

        return table;
    }

    [Fact]
    public void SelectCommand_EmptyString_ReturnsMethod()
    {
        var commandMethod = new CommandMethod(
            command: "",
            (_, _) => new ValueTask<ICommandResult>(EmptyResult.Empty),
            parameters: []
        );

        var commandTable = new Mock<ICommandTable>();
        commandTable.Setup(c => c.TryGetValue("", out commandMethod)).Returns(true);
        commandTable.Setup(c => c.TryGetValue("test", out commandMethod)).Returns(true);

        var commandSelector = new DefaultCommandSelector();

        var result = commandSelector.TrySelect(Tokenize(""), commandTable.Object, out var match);

        Assert.True(result);
        Assert.Equal(commandMethod.Id, match.Method.Id);
        Assert.Equal(0, match.ArgumentStart);
    }

    [Fact]
    public void SelectCommand_RootRouteWithParameter_ReturnsMethod()
    {
        var commandMethod = new CommandMethod(
            "test",
            (_, _) => new ValueTask<ICommandResult>(EmptyResult.Empty),
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

        var result = commandSelector.TrySelect(Tokenize("bacon"), commandTable.Object, out var match);

        Assert.True(result);
        Assert.Equal(commandMethod.Id, match.Method.Id);
        Assert.Equal(0, match.ArgumentStart);
    }

    [Fact]
    public void SelectCommand_UnknownCommand_ReturnsFalse()
    {
        var commandTable = new Mock<ICommandTable>();

        var commandSelector = new DefaultCommandSelector();

        var result = commandSelector.TrySelect(Tokenize("nope"), commandTable.Object, out _);

        Assert.False(result);
    }

    [Fact]
    public void SelectCommand_QuotedArgument_ReturnsArgumentStart()
    {
        var commandMethod = new CommandMethod(
            "say",
            (_, _) => new ValueTask<ICommandResult>(EmptyResult.Empty),
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

        var tokenized = Tokenize("say \"hello world\"");
        var result = commandSelector.TrySelect(tokenized, commandTable.Object, out var match);

        Assert.True(result);
        Assert.Equal(1, match.ArgumentStart);
        Assert.Equal("hello world", Assert.Single(tokenized.GetArguments(match.ArgumentStart)));
    }

    [Theory]
    [InlineData("user add bob", "user add", 2)]
    [InlineData("user remove bob", "user", 1)]
    [InlineData("user add admin bob", "user add admin", 3)]
    [InlineData("user add admin", "user add admin", 3)]
    [InlineData("user", "user", 1)]
    [InlineData("user    add", "user add", 2)]
    public void SelectCommand_CommandTable_SelectsLongestMatchingCommand(string input, string expectedCommand,
        int expectedArgumentStart)
    {
        var table = CreateTable("user", "user add", "user add admin");

        var result = new DefaultCommandSelector().TrySelect(Tokenize(input), table, out var match);

        Assert.True(result);
        Assert.Equal(expectedCommand, match.Method.Command);
        Assert.Equal(expectedArgumentStart, match.ArgumentStart);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("other")]
    [InlineData("")]
    public void SelectCommand_CommandTable_NoMatchingCommand_ReturnsFalse(string input)
    {
        var table = CreateTable("user add");

        var result = new DefaultCommandSelector().TrySelect(Tokenize(input), table, out _);

        Assert.False(result);
    }

    [Fact]
    public void SelectCommand_CommandTable_FallsBackToRootRoute()
    {
        var table = CreateTable("", "user");

        var result = new DefaultCommandSelector().TrySelect(Tokenize("bacon eggs"), table, out var match);

        Assert.True(result);
        Assert.Equal("", match.Method.Command);
        Assert.Equal(0, match.ArgumentStart);
    }

    [Fact]
    public void SelectCommand_CommandTable_CommandAddedAfterFirstSelect_IsSelected()
    {
        var table = CreateTable("user");
        var selector = new DefaultCommandSelector();

        Assert.False(selector.TrySelect(Tokenize("ping"), table, out _));

        table.AddCommand("ping", (_, _) => EmptyResult.Empty);

        Assert.True(selector.TrySelect(Tokenize("ping"), table, out var match));
        Assert.Equal("ping", match.Method.Command);
    }

    [Fact]
    public void GetArguments_ReturnsTokenValuesFromIndex()
    {
        var tokenized = Tokenize("user add \"bob smith\" admin");

        Assert.Equal(["bob smith", "admin"], tokenized.GetArguments(2));
        Assert.Empty(tokenized.GetArguments(4));
    }
}