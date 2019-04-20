namespace CommandRouter.Activation
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public class CommandActivatorScope : ICommandActivatorScope, ICommandActivator
    {
        private readonly IServiceScope _serviceScope;

        public CommandActivatorScope(IServiceScope serviceScope)
        {
            _serviceScope = serviceScope;
        }

        public ICommandActivator CommandActivator => this;

        public object Create(Type type)
        {
            return _serviceScope.ServiceProvider.GetService(type);
        }

        public ICommandActivatorScope CreateScope()
        {
            return new CommandActivatorScope(_serviceScope.ServiceProvider.CreateScope());
        }

        public void Dispose()
        {
            _serviceScope.Dispose();
        }
    }
}