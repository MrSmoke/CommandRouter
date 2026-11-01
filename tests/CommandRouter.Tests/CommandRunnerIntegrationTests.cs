namespace CommandRouter.Tests;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Exceptions;
using Extensions;
using Microsoft.Extensions.DependencyInjection;
using Results;
using Xunit;

public class CommandRunnerIntegrationTests
{
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
    public async Task RunAsync_VoidReturn_NotNullableParameterNull_ThrowsParameterBindingException()
    {
        var commandRunner = Setup();

        var ex = await Assert.ThrowsAsync<ParameterBindingException>(() => commandRunner.RunAsync("void-return-param"));

        Assert.Equal("commandParam", ex.ParameterName);
    }

    [Fact]
    public async Task RunAsync_UserConverter_IsUsedBeforeBuiltInConverter()
    {
        var commandRunner = Setup(o => o.PropertyConverters.Add(new CustomValueConverter()));

        var result = await commandRunner.RunAsync("converter-custom abc");

        using var stream = new MemoryStream();
        await result.ExecuteAsync(stream);
        Assert.Equal("custom:abc", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task RunAsync_NoConverterForParameterType_ThrowsParameterBindingException()
    {
        var commandRunner = Setup();

        await Assert.ThrowsAsync<ParameterBindingException>(() => commandRunner.RunAsync("converter-custom abc"));
    }

    private static ICommandRunner Setup(Action<CommandRouterOptions>? configure = null)
    {
        var serviceCollection = new ServiceCollection();

        if (configure is null)
            serviceCollection.AddCommandRouter(typeof(CommandRunnerIntegrationTests).Assembly);
        else
            serviceCollection.AddCommandRouter(typeof(CommandRunnerIntegrationTests).Assembly, configure);

        var sp = serviceCollection.BuildServiceProvider();
        return sp.GetRequiredService<ICommandRunner>();
    }
}
