namespace CommandRouter.Binding
{
    using System.Collections.Generic;
    using Converters;
    using System;
    using System.Reflection;

    internal class ParameterBinder
    {
        private readonly IList<IPropertyConverter> _propertyConverters;

        internal ParameterBinder(IList<IPropertyConverter> propertyConverters)
        {
            _propertyConverters = propertyConverters;
        }

        internal void AddConverter(IPropertyConverter propertyConverter)
        {
            _propertyConverters.Add(propertyConverter);
        }

        internal IList<object> BindParameters(IList<ParameterInfo> parameterInfos, object[] parameters)
        {
            var boundParams = new object[parameterInfos.Count];

            for (var i = 0; i < parameterInfos.Count; i++)
            {
                var pInfo = parameterInfos[i];

                object value;
                if (parameters.Length == 0 || i >= parameters.Length)
                    value = null;
                else
                    value = parameters[i];

                if (i > parameters.Length)
                {
                    boundParams[i] = null;
                    continue;
                }

                boundParams[i] = ConvertParameter(pInfo, value);
            }

            return boundParams;
        }

        private object ConvertParameter(ParameterInfo paramInfo, object value)
        {
            if (value == null && paramInfo.HasDefaultValue)
                return paramInfo.DefaultValue;

            for (var i = 0; i < _propertyConverters.Count; i++)
            {
                var converter = _propertyConverters[i];

                if (!converter.CanConvert(paramInfo.Type, value))
                    continue;

                return converter.Convert(paramInfo.Type, value);
            }

            //No converters
            if(paramInfo.HasDefaultValue)
                return paramInfo.DefaultValue;

            return null;
        }

        public object GetDefault(Type t)
        {
            if (t.GetTypeInfo().IsValueType)
                return Activator.CreateInstance(t);

            return null;
        }
    }
}
