namespace CommandRouter
{
    using System.Collections.Generic;
    using System.Reflection;
    using Commands;

    public class ApplicationManager
    {
        internal CommandFeatureProvider CommandFeatureProvider { get; set; }

        internal IList<Assembly> Assembiles = new List<Assembly>();
    }
}
