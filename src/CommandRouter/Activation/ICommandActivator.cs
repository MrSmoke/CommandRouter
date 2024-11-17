namespace CommandRouter.Activation
{
    using System;

    public interface ICommandActivator
    {
        object? Create(Type type);
        ICommandActivatorScope CreateScope();
    }
}
