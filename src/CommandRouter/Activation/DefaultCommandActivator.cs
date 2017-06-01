namespace CommandRouter.Activation
{
    using System;

    public class DefaultCommandActivator : ICommandActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultCommandActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object Create(Type type)
        {
            return _serviceProvider.GetService(type);
        }
    }
}
