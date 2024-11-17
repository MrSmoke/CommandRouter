namespace CommandRouter.Activation
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public class DefaultCommandActivator : ICommandActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultCommandActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object? Create(Type type)
        {
            return _serviceProvider.GetService(type);
        }

        public ICommandActivatorScope CreateScope()
        {
            return new CommandActivatorScope(_serviceProvider.CreateScope());
        }
    }
}
