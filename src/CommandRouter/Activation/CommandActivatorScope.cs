namespace CommandRouter.Activation;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public class CommandActivatorScope(AsyncServiceScope serviceScope) : ICommandActivatorScope, ICommandActivator
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
        GC.SuppressFinalize(this);
        serviceScope.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return serviceScope.DisposeAsync();
    }
}
