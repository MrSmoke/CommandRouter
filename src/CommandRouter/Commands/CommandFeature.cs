namespace CommandRouter.Commands;

using System.Collections.Generic;
using System.Reflection;

internal sealed class CommandFeature
{
    public IList<TypeInfo> Commands { get; } = new List<TypeInfo>();
}