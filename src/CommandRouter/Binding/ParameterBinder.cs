namespace CommandRouter.Binding
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Converters;

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

        internal object?[] BindParameters(ParameterInfo[] parameterInfos, object[] parameters)
        {
            var boundParams = new object?[parameterInfos.Length];

            for (var i = 0; i < parameterInfos.Length; i++)
            {
                var pInfo = parameterInfos[i];

                object? value;
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

        private object? ConvertParameter(ParameterInfo paramInfo, object? value)
        {
            // Nullable support
            if (value is null)
            {
                // When the value is null and we have a default value, use that
                if (paramInfo.HasDefaultValue)
                    return paramInfo.DefaultValue;

                // If we don't have a default value, check if we are allowed to be nullable
                // If not, throw
                if (!paramInfo.IsNullable)
                    throw new ArgumentNullException(paramInfo.Name);
            }

            for (var i = 0; i < _propertyConverters.Count; i++)
            {
                var converter = _propertyConverters[i];

                if (value is null)
                    continue;

                if (!converter.CanConvert(paramInfo.Type, value))
                    continue;

                return converter.Convert(paramInfo.Type, value);
            }

            //No converters
            return paramInfo.HasDefaultValue ? paramInfo.DefaultValue : null;
        }
    }
}
