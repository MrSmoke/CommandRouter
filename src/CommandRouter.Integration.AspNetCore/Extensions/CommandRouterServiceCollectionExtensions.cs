namespace CommandRouter.Integration.AspNetCore.Extensions
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using CommandRouter.Extensions;
    using Microsoft.AspNetCore.Hosting;

    public static class CommandRouterServiceCollectionExtensions
    {
        public static void AddCommandRouter(this IServiceCollection services)
        {
            services.AddCommandRouter(GetEntryAssembly(services));
        }

        public static void AddCommandRouter(this IServiceCollection services, Action<CommandRouterOptions> optionsFunc)
        {
            services.AddCommandRouter(GetEntryAssembly(services), optionsFunc);
        }

        private static Assembly GetEntryAssembly(IServiceCollection services)
        {
            var environment = (IHostingEnvironment) services
                .FirstOrDefault(d => d.ServiceType == typeof(IHostingEnvironment))
                ?.ImplementationInstance;

            if (string.IsNullOrWhiteSpace(environment?.ApplicationName))
                throw new Exception("Failed to get entry point assembly name");

            return Assembly.Load(new AssemblyName(environment.ApplicationName));
        }
    }
}
