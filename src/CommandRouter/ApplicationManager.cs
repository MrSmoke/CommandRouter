namespace CommandRouter
{
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

        // The assemblies are scanned once, then shared by DI registration and the command table.
        // Assemblies must be fully populated before the first call
        internal CommandFeature GetCommandFeature()
        {
            if (_commandFeature != null)
                return _commandFeature;

            var feature = new CommandFeature();
            PopulateFeature(feature);

            return _commandFeature ??= feature;
        }
    }
}
