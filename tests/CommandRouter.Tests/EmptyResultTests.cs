namespace CommandRouter.Tests;

using Results;
using Xunit;

public class EmptyResultTests
{
    [Fact]
    public void Empty_ReturnsSameInstanceOnEveryAccess()
    {
        Assert.Same(EmptyResult.Empty, EmptyResult.Empty);
    }
}
