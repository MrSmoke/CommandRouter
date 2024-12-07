namespace CommandRouter.Tests;

using System;
using System.Threading.Tasks;
using Extensions;
using Microsoft.Extensions.DependencyInjection;
using Results;
using Xunit;

public class CommandRunnerIntegrationTests
{
    public CommandRunnerIntegrationTests()
    {
        _ = new TestCommand();
    }

    [Theory]
    [InlineData("void-return-null-param")]
    [InlineData("void-return-param testvalue")]
    [InlineData("void-return-int-param 999")]
    public async Task RunAsync_VoidReturn_ReturnsEmptyResult(string command)
    {
        var commandRunner = Setup();

        var result = await commandRunner.RunAsync(command);

        Assert.IsType<EmptyResult>(result);
    }

    [Fact]
    public async Task RunAsync_VoidReturn_NotNullableParameterNull_ThrowsArgumentNullException()
    {
        var commandRunner = Setup();

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => commandRunner.RunAsync("void-return-param"));

        Assert.Equal("commandParam", ex.ParamName);
    }

    private static ICommandRunner Setup()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddCommandRouter(typeof(CommandRunnerIntegrationTests).Assembly);

        var sp = serviceCollection.BuildServiceProvider();
        return sp.GetRequiredService<ICommandRunner>();
    }
}
