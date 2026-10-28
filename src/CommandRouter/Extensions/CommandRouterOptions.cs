namespace CommandRouter.Extensions;

using System.Collections.Generic;
using Converters;

public class CommandRouterOptions
{
    /// <summary>
    /// Custom property converters.
    /// They are checked in order, before the built-in <see cref="SystemTypeConverter"/>.
    /// </summary>
    public List<IPropertyConverter> PropertyConverters { get; } = [];
}
