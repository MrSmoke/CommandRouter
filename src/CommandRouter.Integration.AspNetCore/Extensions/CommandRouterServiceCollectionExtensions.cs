namespace CommandRouter.Integration.AspNetCore.Extensions
{
    using System;
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using CommandRouter.Extensions;

    public static class CommandRouterServiceCollectionExtensions
    {
        public static void AddCommandRouterForAssembly(this IServiceCollection services, Assembly assembly)
        {
            services.AddCommandRouter(assembly);
        }

        public static void AddCommandRouterForAssembly(this IServiceCollection services, Assembly assembly,
            Action<CommandRouterOptions> optionsFunc)
        {
            services.AddCommandRouter(assembly, optionsFunc);
        }
    }
}
