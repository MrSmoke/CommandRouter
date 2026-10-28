namespace CommandRouter.Binding;

using System;
using System.Collections.Generic;
using Converters;
using Exceptions;

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

    internal object?[] BindParameters(ParameterInfo[] parameterInfos, string[] parameters)
    {
        var boundParams = new object?[parameterInfos.Length];

        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var pInfo = parameterInfos[i];

            var value = i < parameters.Length ? parameters[i] : null;

            boundParams[i] = ConvertParameter(pInfo, value);
        }

        return boundParams;
    }

    private object? ConvertParameter(ParameterInfo paramInfo, string? value)
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

        // Nullable parameter without a supplied value
        if (value is null)
            return null;

        for (var i = 0; i < _propertyConverters.Count; i++)
        {
            var converter = _propertyConverters[i];

            if (!converter.CanConvert(paramInfo.Type, value))
                continue;

            return converter.Convert(paramInfo.Type, value);
        }

        throw new CommandRouterException(
            $"No converter can convert '{value}' to {paramInfo.Type} for parameter '{paramInfo.Name}'");
    }
}