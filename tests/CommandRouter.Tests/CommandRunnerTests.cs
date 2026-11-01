namespace CommandRouter.Tests;

using System;
using System.Threading.Tasks;
using Exceptions;
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
        commandSelector.Setup(c => c.TrySelect(It.Is<TokenisedCommand>(t => t.Tokens.Count == 1 && t.Tokens[0].Value == "test"), commandTable.Object, out outMatch)).Returns(true);

        var pBinder = TestBinder.Create();

        var commandRunner = new CommandRunner(commandTable.Object, new DefaultCommandTokeniser(), commandSelector.Object, pBinder);

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

        var commandRunner = new CommandRunner(commandTable.Object, new DefaultCommandTokeniser(), new DefaultCommandSelector(), TestBinder.Create());

        var ex = await Assert.ThrowsAsync<InvalidTokenException>(() => commandRunner.RunAsync("say \"hello"));

        Assert.Equal(4, ex.Position);
    }

    [Fact]
    public async Task RunAsync_UsesCustomTokeniser()
    {
        var commandMethod = new CommandMethod(
            command: "say",
            action: (_, _) => new ValueTask<ICommandResult>(EmptyResult.Empty),
            parameters: []
        );

        var commandTable = new Mock<ICommandTable>();
        commandTable.Setup(c => c.TryGetValue("say", out commandMethod)).Returns(true);

        var custom = new TokenisedCommand("say", [new CommandToken("say", 0, 3, false)]);
        var tokeniser = new Mock<ICommandTokeniser>();
        tokeniser.Setup(t => t.TryTokenise("anything", out custom, out It.Ref<int>.IsAny)).Returns(true);

        var commandRunner = new CommandRunner(commandTable.Object, tokeniser.Object, new DefaultCommandSelector(), TestBinder.Create());

        await commandRunner.RunAsync("anything");

        tokeniser.Verify(t => t.TryTokenise("anything", out custom, out It.Ref<int>.IsAny), Times.Once);
    }

    [Fact]
    public async Task RunAsync_UnknownCommand_ThrowsCommandNotFoundException()
    {
        var commandTable = new Mock<ICommandTable>();

        var commandRunner = new CommandRunner(commandTable.Object, new DefaultCommandTokeniser(), new DefaultCommandSelector(), TestBinder.Create());

        await Assert.ThrowsAsync<CommandNotFoundException>(() => commandRunner.RunAsync("nope"));
    }

    private class TestException : Exception;
}