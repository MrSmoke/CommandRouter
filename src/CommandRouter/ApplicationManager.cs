namespace CommandRouter;

using System.Collections.Generic;
using System.Reflection;
using Commands;

public class ApplicationManager
{
    private CommandFeature? _commandFeature;

    internal readonly IList<Assembly> Assemblies = new List<Assembly>();

    internal void PopulateFeature(CommandFeature feature)
    {
        DefaultCommandFeatureProvider.PopulateFeature(Assemblies, feature);
    }

    /// <summary>
    /// Scans <see cref="Assemblies"/> for commands on the first call, then returns the cached result
    /// </summary>
    internal CommandFeature GetCommandFeature()
    {
        if (_commandFeature != null)
            return _commandFeature;

        var feature = new CommandFeature();
        PopulateFeature(feature);

        return _commandFeature ??= feature;
    }
}