namespace CommandRouter.Activation;

using System;
using Microsoft.Extensions.DependencyInjection;

public class CommandActivatorScope(IServiceScope serviceScope) : ICommandActivatorScope, ICommandActivator
{
    public ICommandActivator CommandActivator => this;

    public object? Create(Type type)
    {
        return serviceScope.ServiceProvider.GetService(type);
    }

    public ICommandActivatorScope CreateScope()
    {
        return new CommandActivatorScope(serviceScope.ServiceProvider.CreateScope());
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        serviceScope.Dispose();
    }
}