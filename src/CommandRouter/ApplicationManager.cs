namespace CommandRouter
{
    using System.Collections.Generic;
    using System.Reflection;
    using Commands;

    internal class ApplicationManager
    {
        internal readonly IList<Assembly> Assemblies = new List<Assembly>();

        internal void PopulateFeature(CommandFeature feature)
        {
            DefaultCommandFeatureProvider.PopulateFeature(Assemblies, feature);
        }
    }
}
