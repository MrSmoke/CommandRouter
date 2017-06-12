namespace CommandRouter.Binding
{
    using System;

    public class ParameterInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public object DefaultValue { get; set; }
        public bool HasDefaultValue { get; set; }
    }
}