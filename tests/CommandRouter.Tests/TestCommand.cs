namespace CommandRouter.Tests;

using Attributes;
using Commands;
using Xunit;

#pragma warning disable CA1822
public class TestCommand : Command
{
    [Command("void-return-null-param")]
    public void VoidReturnNullParam(string? commandParam)
    {
        Assert.Null(commandParam);
    }

    [Command("void-return-param")]
    public void VoidReturnParam(string commandParam)
    {
        Assert.Equal("testvalue", commandParam);
    }

    [Command("void-return-int-param")]
    public void VoidReturnParam(int intParam)
    {
        Assert.Equal(999, intParam);
    }
}
