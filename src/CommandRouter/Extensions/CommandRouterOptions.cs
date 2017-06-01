namespace CommandRouter.Extensions
{
    using System.Collections.Generic;
    using Converters;

    public class CommandRouterOptions
    {
        public List<IPropertyConverter> PropertyConverters { get; } = new List<IPropertyConverter>();

        public CommandRouterOptions()
        {
            //Default converter
            PropertyConverters.Add(new SystemTypeConverter());
        }
    }
}
