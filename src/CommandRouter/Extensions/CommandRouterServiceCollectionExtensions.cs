namespace CommandRouter.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Activation;
using Binding;
using Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;
using Routing;

public static class CommandRouterServiceCollectionExtensions
{
    private static HashSet<string> ReferenceAssemblies { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "CommandRouter"
    };

    /// <summary>
    /// Adds the command router using the entry assembly as the command source.
    /// </summary>
    public static IServiceCollection AddCommandRouter(this IServiceCollection services)
    {
        return services.AddCommandRouter(GetEntryAssembly());
    }

    public static IServiceCollection AddCommandRouter(this IServiceCollection services,
        Action<CommandRouterOptions> optionsFunc)
    {
        return services.AddCommandRouter(GetEntryAssembly(), optionsFunc);
    }

    public static IServiceCollection AddCommandRouter(this IServiceCollection services, Assembly entryAssembly)
    {
        return AddCore(services, entryAssembly, new CommandRouterOptions());
    }

    public static IServiceCollection AddCommandRouter(this IServiceCollection services, Assembly entryAssembly,
        Action<CommandRouterOptions> optionsFunc)
    {
        ArgumentNullException.ThrowIfNull(optionsFunc);

        var options = new CommandRouterOptions();
        optionsFunc(options);

        return AddCore(services, entryAssembly, options);
    }

    /// <summary>
    /// Builds the command table now, so registration problems (duplicate commands, unsupported return types, etc)
    /// are reported at startup rather than on the first command.
    /// </summary>
    public static IServiceProvider ValidateCommandRouter(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _ = provider.GetRequiredService<ICommandTable>();
        return provider;
    }

    // Options passed to later calls are ignored if the router was already registered
    private static IServiceCollection AddCore(IServiceCollection services, Assembly entryAssembly,
        CommandRouterOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(entryAssembly);

        var manager = GetApplicationManager(services, entryAssembly);
        services.TryAddSingleton(manager);

        RegisterCommands(services, manager);

        services.TryAddSingleton<ICommandTable>(sp => new CommandTable(
            sp.GetRequiredService<ICommandActivator>(),
            sp.GetRequiredService<ApplicationManager>()));

        services.TryAddSingleton<ICommandActivator, DefaultCommandActivator>();

        services.TryAddSingleton<ICommandRunner, CommandRunner>();

        services.TryAddSingleton<ICommandTokenizer, DefaultCommandTokenizer>();

        services.TryAddSingleton<ICommandSelector, DefaultCommandSelector>();

        //user converters first, the built-in converter is the fallback
        var converters = new List<IPropertyConverter>(options.PropertyConverters) { new SystemTypeConverter() };

        services.TryAddSingleton(new ParameterBinder(converters));

        return services;
    }

    private static Assembly GetEntryAssembly()
    {
        return Assembly.GetEntryAssembly() ??
               throw new InvalidOperationException(
                   "The entry assembly could not be determined, pass the assembly explicitly.");
    }

    private static ApplicationManager GetApplicationManager(IServiceCollection services, Assembly entryAssembly)
    {
        var manager = GetServiceFromCollection<ApplicationManager>(services);
        if (manager != null)
            return manager;

        manager = new ApplicationManager();

        var context = DependencyContext.Load(entryAssembly);

        foreach (var assembly in GetCandidateAssemblies(entryAssembly, context))
            manager.Assemblies.Add(assembly);

        return manager;
    }

    private static void RegisterCommands(IServiceCollection services, ApplicationManager manager)
    {
        //register command classes in DI
        foreach (var command in manager.GetCommandFeature().Commands)
        {
            services.TryAddTransient(command.AsType());
        }
    }

    private static T? GetServiceFromCollection<T>(IServiceCollection services)
    {
        return (T?)services
            .FirstOrDefault(d => d.ServiceType == typeof(T))
            ?.ImplementationInstance;
    }

    private static IEnumerable<Assembly> GetCandidateAssemblies(Assembly entryAssembly, DependencyContext? dependencyContext)
    {
        if (dependencyContext == null)
        {
            // Use the entry assembly as the sole candidate.
            return [entryAssembly];
        }

        return GetCandidateLibraries(dependencyContext)
            .SelectMany(library => library.GetDefaultAssemblyNames(dependencyContext))
            .Select(Assembly.Load);
    }

    private static IEnumerable<RuntimeLibrary> GetCandidateLibraries(DependencyContext dependencyContext)
    {
        var candidatesResolver = new CandidateResolver(dependencyContext.RuntimeLibraries, ReferenceAssemblies);
        return candidatesResolver.GetCandidates();
    }

    private class CandidateResolver
    {
        private readonly Dictionary<string, Dependency> _dependencies;

        public CandidateResolver(IReadOnlyList<RuntimeLibrary> dependencies, ISet<string> referenceAssemblies)
        {
            var dependenciesWithNoDuplicates = new Dictionary<string, Dependency>(StringComparer.OrdinalIgnoreCase);
            foreach (var dependency in dependencies)
            {
                if (dependenciesWithNoDuplicates.ContainsKey(dependency.Name))
                {
                    throw new InvalidOperationException(
                        $"The dependency context contains the library '{dependency.Name}' more than once.");
                }
                dependenciesWithNoDuplicates.Add(dependency.Name, CreateDependency(dependency, referenceAssemblies));
            }

            _dependencies = dependenciesWithNoDuplicates;
        }

        private static Dependency CreateDependency(RuntimeLibrary library, ISet<string> referenceAssemblies)
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
            // A dependency that isn't part of the context can't lead to a candidate
            if (!_dependencies.TryGetValue(dependency, out var candidateEntry))
                return DependencyClassification.NotCandidate;

            if (candidateEntry.Classification != DependencyClassification.Unknown)
            {
                return candidateEntry.Classification;
            }

            var classification = DependencyClassification.NotCandidate;
            foreach (var candidateDependency in candidateEntry.Library.Dependencies)
            {
                var dependencyClassification = ComputeClassification(candidateDependency.Name);
                if (dependencyClassification is DependencyClassification.Candidate or DependencyClassification.MvcReference)
                {
                    classification = DependencyClassification.Candidate;
                    break;
                }
            }

            candidateEntry.Classification = classification;

            return classification;
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

        private class Dependency(RuntimeLibrary library, DependencyClassification classification)
        {
            public RuntimeLibrary Library { get; } = library;

            public DependencyClassification Classification { get; set; } = classification;

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
