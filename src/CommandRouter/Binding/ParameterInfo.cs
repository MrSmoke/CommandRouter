namespace CommandRouter.Binding;

using System;

internal sealed class ParameterInfo
{
    public string? Name { get; init; }
    public required Type Type { get; init; }
    public object? DefaultValue { get; init; }
    public bool HasDefaultValue { get; init; }

    /// <summary>
    /// If true, the property is allowed to be null
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// If true, the parameter is a params array that receives all the remaining arguments
    /// </summary>
    public bool IsParams { get; init; }
}