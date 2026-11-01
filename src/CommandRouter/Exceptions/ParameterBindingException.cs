namespace CommandRouter.Exceptions;

using System;

/// <summary>
/// Thrown when a command argument is missing or can't be converted to its parameter type
/// </summary>
public class ParameterBindingException : CommandRouterException
{
    public ParameterBindingException(string message, string? parameterName, string? value)
        : base(message)
    {
        ParameterName = parameterName;
        Value = value;
    }

    public ParameterBindingException(string message, string? parameterName, string? value,
        Exception innerException)
        : base(message, innerException)
    {
        ParameterName = parameterName;
        Value = value;
    }

    public string? ParameterName { get; }

    /// <summary>
    /// The argument supplied, or null if it was missing
    /// </summary>
    public string? Value { get; }
}
