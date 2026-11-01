namespace CommandRouter.Activation;

using System;

public interface ICommandActivatorScope : IDisposable, IAsyncDisposable
{
    ICommandActivator CommandActivator { get; }
}
