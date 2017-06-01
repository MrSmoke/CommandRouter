namespace CommandRouter.Commands
{
    using System.Collections.Generic;
    using System.Reflection;

    public class CommandFeature
    {
        public IList<TypeInfo> Commands { get; } = new List<TypeInfo>();
    }
}
