namespace CommandRouter.Activation;

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

internal static class CommandFactory
{
    private static readonly ConcurrentDictionary<Type, ObjectFactory> Factories = new();

    /// <summary>
    /// Resolves <paramref name="type"/> if it's registered, otherwise creates it with its dependencies from
    /// <paramref name="provider"/>
    /// </summary>
    public static object Create(IServiceProvider provider, Type type)
    {
        return provider.GetService(type) ??
               Factories.GetOrAdd(type, static t => ActivatorUtilities.CreateFactory(t, Type.EmptyTypes))(provider, null);
    }
}
