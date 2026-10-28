namespace CommandRouter.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal static class DefaultCommandFeatureProvider
    {
        public static void PopulateFeature(IEnumerable<Assembly> assemblies, CommandFeature commandFeature)
        {
            var seen = new HashSet<Type>(commandFeature.Commands);

            foreach (var assembly in assemblies)
            {
                foreach (var type in GetTypes(assembly))
                {
                    if (IsCommand(type) && seen.Add(type))
                        commandFeature.Commands.Add(type);
                }
            }
        }

        private static IEnumerable<TypeInfo> GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.DefinedTypes;
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Use the types that did load
                return ex.Types.Where(t => t != null).Select(t => t!.GetTypeInfo());
            }
        }

        private static bool IsCommand(Type typeInfo)
        {
            if (!typeInfo.IsClass)
                return false;

            if (typeInfo.IsAbstract)
                return false;

            if (!typeInfo.IsPublic)
                return false;

            if (typeInfo.ContainsGenericParameters)
                return false;

            if (!typeInfo.IsSubclassOf(typeof(Command)))
                return false;

            if (!typeInfo.Name.EndsWith("Command", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
    }
}
