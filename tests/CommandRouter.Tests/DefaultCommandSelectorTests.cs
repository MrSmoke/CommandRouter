namespace CommandRouter.Tests;

using System.Collections.Generic;
using System.Threading.Tasks;
using CommandRouter.Binding;
using Moq;
using Results;
using Routing;
using Xunit;

public class DefaultCommandSelectorTests
{
    private static TokenisedCommand Tokenise(string str)
    {
        Assert.True(new DefaultCommandTokeniser().TryTokenise(str, out var command, out _));
        return command;
    }

    private static FakeCommandTable CreateTable(params string[] commands)
    {
        var table = new FakeCommandTable();

        foreach (var command in commands)
            table.Add(command, new CommandMethod(command, (_, _) => new ValueTask<ICommandResult>(EmptyResult.Empty), []));

        return table;
    }

    private sealed class FakeCommandTable : Dictionary<string, CommandMethod>, ICommandTable;

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

        var result = commandSelector.TrySelect(Tokenise(""), commandTable.Object, out var match);

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

        var result = commandSelector.TrySelect(Tokenise("bacon"), commandTable.Object, out var match);

        Assert.True(result);
        Assert.Equal(commandMethod.Id, match.Method.Id);
        Assert.Equal(0, match.ArgumentStart);
    }

    [Fact]
    public void SelectCommand_UnknownCommand_ReturnsFalse()
    {
        var commandTable = new Mock<ICommandTable>();

        var commandSelector = new DefaultCommandSelector();

        var result = commandSelector.TrySelect(Tokenise("nope"), commandTable.Object, out _);

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

        var tokenised = Tokenise("say \"hello world\"");
        var result = commandSelector.TrySelect(tokenised, commandTable.Object, out var match);

        Assert.True(result);
        Assert.Equal(1, match.ArgumentStart);
        Assert.Equal("hello world", Assert.Single(tokenised.GetArguments(match.ArgumentStart)));
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

        var result = new DefaultCommandSelector().TrySelect(Tokenise(input), table, out var match);

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

        var result = new DefaultCommandSelector().TrySelect(Tokenise(input), table, out _);

        Assert.False(result);
    }

    [Theory]
    [InlineData("user \"add\"", "user", 1)]
    [InlineData("user add \"bob\"", "user add", 2)]
    [InlineData("user \"add\" bob", "user", 1)]
    public void SelectCommand_QuotedToken_IsNeverPartOfCommandName(string input, string expectedCommand,
        int expectedArgumentStart)
    {
        var table = CreateTable("user", "user add");

        var result = new DefaultCommandSelector().TrySelect(Tokenise(input), table, out var match);

        Assert.True(result);
        Assert.Equal(expectedCommand, match.Method.Command);
        Assert.Equal(expectedArgumentStart, match.ArgumentStart);
    }

    [Theory]
    [InlineData("\"user add\" bob")]
    [InlineData("\"\" x")]
    public void SelectCommand_QuotedFirstToken_FallsBackToRootRoute(string input)
    {
        var table = CreateTable("", "user add");

        var result = new DefaultCommandSelector().TrySelect(Tokenise(input), table, out var match);

        Assert.True(result);
        Assert.Equal("", match.Method.Command);
        Assert.Equal(0, match.ArgumentStart);
    }

    [Fact]
    public void SelectCommand_QuotedFirstToken_NoRootRoute_ReturnsFalse()
    {
        var table = CreateTable("user add");

        Assert.False(new DefaultCommandSelector().TrySelect(Tokenise("\"user add\" bob"), table, out _));
    }

    [Fact]
    public void SelectCommand_CommandTable_FallsBackToRootRoute()
    {
        var table = CreateTable("", "user");

        var result = new DefaultCommandSelector().TrySelect(Tokenise("bacon eggs"), table, out var match);

        Assert.True(result);
        Assert.Equal("", match.Method.Command);
        Assert.Equal(0, match.ArgumentStart);
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(1, "user")]
    [InlineData(2, "user add")]
    public void GetCommandName_JoinsLeadingTokenValuesWithSingleSpaces(int count, string expected)
    {
        Assert.Equal(expected, Tokenise("user\t add  bob").GetCommandName(count));
    }

    [Fact]
    public void GetArguments_ReturnsTokenValuesFromIndex()
    {
        var tokenised = Tokenise("user add \"bob smith\" admin");

        Assert.Equal(["bob smith", "admin"], tokenised.GetArguments(2));
        Assert.Empty(tokenised.GetArguments(4));
    }
}