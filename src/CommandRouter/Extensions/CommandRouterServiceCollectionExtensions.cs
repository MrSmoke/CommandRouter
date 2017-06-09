namespace CommandRouter.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Activation;
    using Binding;
    using Commands;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.DependencyModel;
    using Routing;

    public static class CommandRouterServiceCollectionExtensions
    {
        internal static HashSet<string> ReferenceAssemblies { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CommandRouter"
        };

        public static void AddCommandRouter(this IServiceCollection services, Assembly entryAssembly)
        {
            AddCommandRouter(services, entryAssembly, new CommandRouterOptions());
        }

        public static void AddCommandRouter(this IServiceCollection services, Assembly entryAssembly, Action<CommandRouterOptions> optionsFunc)
        {
            var options = new CommandRouterOptions();
            optionsFunc(options);

            AddCommandRouter(services, entryAssembly, options);
        }

        private static void AddCommandRouter(this IServiceCollection services, Assembly entryAssembly, CommandRouterOptions options)
        {
            var manager = GetApplicationManager(services, entryAssembly);
            services.AddSingleton(manager);

            RegisterCommands(services, manager);

            services.AddSingleton<CommandTable>();

            services.AddSingleton<ICommandActivator, DefaultCommandActivator>();

            services.AddSingleton<ICommandRunner, CommandRunner>();

            services.AddSingleton<ICommandSelector, DefaultCommandSelector>();

            var paramBinder = new ParameterBinder(options.PropertyConverters);

            services.AddSingleton(paramBinder);
        }

        private static ApplicationManager GetApplicationManager(IServiceCollection services, Assembly entryAssembly)
        {
            var manager = GetServiceFromCollection<ApplicationManager>(services);
            if (manager != null)
                return manager;

            manager = new ApplicationManager();

            var context = DependencyContext.Load(entryAssembly);

            foreach(var assembly in GetCandidateAssemblies(entryAssembly, context))
                manager.Assembiles.Add(assembly);

            return manager;
        }

        private static void RegisterCommands(IServiceCollection services, ApplicationManager manager)
        {
            //Register command feature
            manager.CommandFeatureProvider = new CommandFeatureProvider();

            var commandFeature = new CommandFeature();
            manager.PopulateFeature(commandFeature);

            //register command classes in DI
            foreach (var command in commandFeature.Commands)
            {
                services.TryAddTransient(command.AsType());
            }
        }

        private static T GetServiceFromCollection<T>(IServiceCollection services)
        {
            return (T)services
                .FirstOrDefault(d => d.ServiceType == typeof(T))
                ?.ImplementationInstance;
        }

        internal static IEnumerable<Assembly> GetCandidateAssemblies(Assembly entryAssembly, DependencyContext dependencyContext)
        {
            if (dependencyContext == null)
            {
                // Use the entry assembly as the sole candidate.
                return new[] { entryAssembly };
            }

            return GetCandidateLibraries(dependencyContext)
                .SelectMany(library => library.GetDefaultAssemblyNames(dependencyContext))
                .Select(Assembly.Load);
        }

        internal static IEnumerable<RuntimeLibrary> GetCandidateLibraries(DependencyContext dependencyContext)
        {
            if (ReferenceAssemblies == null)
            {
                return Enumerable.Empty<RuntimeLibrary>();
            }

            var candidatesResolver = new CandidateResolver(dependencyContext.RuntimeLibraries, ReferenceAssemblies);
            return candidatesResolver.GetCandidates();
        }

        private class CandidateResolver
        {
            private readonly IDictionary<string, Dependency> _dependencies;

            public CandidateResolver(IReadOnlyList<RuntimeLibrary> dependencies, ISet<string> referenceAssemblies)
            {
                var dependenciesWithNoDuplicates = new Dictionary<string, Dependency>(StringComparer.OrdinalIgnoreCase);
                foreach (var dependency in dependencies)
                {
                    if (dependenciesWithNoDuplicates.ContainsKey(dependency.Name))
                    {
                        throw new InvalidOperationException();
                    }
                    dependenciesWithNoDuplicates.Add(dependency.Name, CreateDependency(dependency, referenceAssemblies));
                }

                _dependencies = dependenciesWithNoDuplicates;
            }

            private Dependency CreateDependency(RuntimeLibrary library, ISet<string> referenceAssemblies)
            {
                var classification = DependencyClassification.Unknown;
                if (referenceAssemblies.Contains(library.Name))
                {
                    classification = DependencyClassification.MvcReference;
                }

                return new Dependency(library, classification);
            }

            private DependencyClassification ComputeClassification(string dependency)
            {
                Debug.Assert(_dependencies.ContainsKey(dependency));

                var candidateEntry = _dependencies[dependency];
                if (candidateEntry.Classification != DependencyClassification.Unknown)
                {
                    return candidateEntry.Classification;
                }
                else
                {
                    var classification = DependencyClassification.NotCandidate;
                    foreach (var candidateDependency in candidateEntry.Library.Dependencies)
                    {
                        var dependencyClassification = ComputeClassification(candidateDependency.Name);
                        if (dependencyClassification == DependencyClassification.Candidate ||
                            dependencyClassification == DependencyClassification.MvcReference)
                        {
                            classification = DependencyClassification.Candidate;
                            break;
                        }
                    }

                    candidateEntry.Classification = classification;

                    return classification;
                }
            }

            public IEnumerable<RuntimeLibrary> GetCandidates()
            {
                foreach (var dependency in _dependencies)
                {
                    if (ComputeClassification(dependency.Key) == DependencyClassification.Candidate)
                    {
                        yield return dependency.Value.Library;
                    }
                }
            }

            private class Dependency
            {
                public Dependency(RuntimeLibrary library, DependencyClassification classification)
                {
                    Library = library;
                    Classification = classification;
                }

                public RuntimeLibrary Library { get; }

                public DependencyClassification Classification { get; set; }

                public override string ToString()
                {
                    return $"Library: {Library.Name}, Classification: {Classification}";
                }
            }

            private enum DependencyClassification
            {
                Unknown = 0,
                Candidate = 1,
                NotCandidate = 2,
                MvcReference = 3
            }
        }
    }
}
