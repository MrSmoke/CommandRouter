namespace CommandRouter;

using System.Collections.Generic;
using System.Reflection;
using Commands;

internal sealed class ApplicationManager
{
    internal readonly IList<Assembly> Assemblies = new List<Assembly>();

    /// <summary>
    /// Scans <see cref="Assemblies"/> for command classes
    /// </summary>
    internal CommandFeature GetCommandFeature()
    {
        var feature = new CommandFeature();
        DefaultCommandFeatureProvider.PopulateFeature(Assemblies, feature);

        return feature;
    }
}