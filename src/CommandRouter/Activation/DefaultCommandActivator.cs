namespace CommandRouter.Activation;

using System;
using Microsoft.Extensions.DependencyInjection;

public sealed class DefaultCommandActivator(IServiceProvider serviceProvider) : ICommandActivator
{
    public object? Create(Type type)
    {
        return CommandFactory.Create(serviceProvider, type);
    }

    public ICommandActivatorScope CreateScope()
    {
        return new CommandActivatorScope(serviceProvider.CreateAsyncScope());
    }
}