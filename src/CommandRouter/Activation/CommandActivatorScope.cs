namespace CommandRouter.Activation;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public sealed class CommandActivatorScope(AsyncServiceScope serviceScope) : ICommandActivatorScope, ICommandActivator
{
    public ICommandActivator CommandActivator => this;

    public object? Create(Type type)
    {
        return CommandFactory.Create(serviceScope.ServiceProvider, type);
    }

    public ICommandActivatorScope CreateScope()
    {
        return new CommandActivatorScope(serviceScope.ServiceProvider.CreateAsyncScope());
    }

    public void Dispose()
    {
        serviceScope.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return serviceScope.DisposeAsync();
    }
}
