namespace CommandRouter.Activation;

using System;
using Microsoft.Extensions.DependencyInjection;

public class DefaultCommandActivator(IServiceProvider serviceProvider) : ICommandActivator
{
    public object? Create(Type type)
    {
        return serviceProvider.GetService(type);
    }

    public ICommandActivatorScope CreateScope()
    {
        return new CommandActivatorScope(serviceProvider.CreateScope());
    }
}