namespace CommandRouter.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class CommandFeatureProvider
    {
        public void PopulateFeature(IEnumerable<Assembly> assemblies, CommandFeature commandFeature)
        {
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.DefinedTypes)
                {
                    if (IsCommand(type) && !commandFeature.Commands.Contains(type))
                        commandFeature.Commands.Add(type);
                }
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
