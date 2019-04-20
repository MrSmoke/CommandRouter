namespace CommandRouter.Activation
{
    using System;

    public interface ICommandActivatorScope : IDisposable
    {
        ICommandActivator CommandActivator { get; }
    }
}