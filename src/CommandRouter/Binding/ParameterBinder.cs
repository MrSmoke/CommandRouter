namespace CommandRouter.Binding;

using System;
using Converters;
using Exceptions;
using Extensions;
using Microsoft.Extensions.Options;

internal sealed class ParameterBinder
{
    private readonly IPropertyConverter[] _propertyConverters;

    /// <summary>
    /// Uses the configured converters, falling back to <see cref="SystemTypeConverter"/>
    /// </summary>
    public ParameterBinder(IOptions<CommandRouterOptions> options)
    {
        _propertyConverters = [..options.Value.PropertyConverters, new SystemTypeConverter()];
    }

    internal object?[] BindParameters(ParameterInfo[] parameterInfos, string[] parameters)
    {
        var boundParams = new object?[parameterInfos.Length];

        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var pInfo = parameterInfos[i];

            // A params array is always last and takes every remaining argument
            if (pInfo.IsParams)
            {
                boundParams[i] = BindParamsArray(pInfo, parameters, i);
                return boundParams;
            }

            var value = i < parameters.Length ? parameters[i] : null;

            boundParams[i] = ConvertParameter(pInfo, value);
        }

        if (parameters.Length > parameterInfos.Length)
        {
            throw new ParameterBindingException(
                $"Too many arguments, expected at most {parameterInfos.Length}", null,
                parameters[parameterInfos.Length]);
        }

        return boundParams;
    }

    private Array BindParamsArray(ParameterInfo paramInfo, string[] parameters, int start)
    {
        var elementInfo = new ParameterInfo { Name = paramInfo.Name, Type = paramInfo.Type.GetElementType()! };
        var array = Array.CreateInstance(elementInfo.Type, Math.Max(parameters.Length - start, 0));

        for (var i = 0; i < array.Length; i++)
            array.SetValue(ConvertParameter(elementInfo, parameters[start + i]), i);

        return array;
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
            {
                throw new ParameterBindingException(
                    $"Missing value for parameter '{paramInfo.Name}'", paramInfo.Name, null);
            }

            // Nullable parameter without a supplied value
            return null;
        }

        for (var i = 0; i < _propertyConverters.Length; i++)
        {
            var converter = _propertyConverters[i];

            if (!converter.CanConvert(paramInfo.Type, value))
                continue;

            try
            {
                return converter.Convert(paramInfo.Type, value);
            }
            catch (Exception ex) when (ex is not ParameterBindingException)
            {
                throw new ParameterBindingException(
                    $"Unable to convert '{value}' to {paramInfo.Type} for parameter '{paramInfo.Name}'",
                    paramInfo.Name, value, ex);
            }
        }

        throw new ParameterBindingException(
            $"No converter can convert '{value}' to {paramInfo.Type} for parameter '{paramInfo.Name}'",
            paramInfo.Name, value);
    }
}