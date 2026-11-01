namespace CommandRouter.Converters;

using System;
using System.Globalization;

public sealed class SystemTypeConverter : IPropertyConverter
{
    public bool CanConvert(Type propertyType, string value)
    {
        var target = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (target == typeof(string) || target == typeof(object) || target.IsEnum || target == typeof(Guid) ||
            target == typeof(TimeSpan) || target == typeof(DateTimeOffset))
            return true;

        return Type.GetTypeCode(target) is not (TypeCode.Object or TypeCode.Empty or TypeCode.DBNull);
    }

    public object Convert(Type propertyType, string value)
    {
        var target = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (target == typeof(string) || target == typeof(object))
            return value;

        if (target.IsEnum)
            return ParseEnum(target, value);

        if (target == typeof(Guid))
            return Guid.Parse(value);

        if (target == typeof(TimeSpan))
            return TimeSpan.Parse(value, CultureInfo.InvariantCulture);

        if (target == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);

        return System.Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
    }

    private static object ParseEnum(Type enumType, string value)
    {
        var result = Enum.Parse(enumType, value, true);

        var isDefined = enumType.IsDefined(typeof(FlagsAttribute), false)
            // Flags with an undefined bit format as a number rather than member names
            ? !IsNumber(result.ToString()!)
            : Enum.IsDefined(enumType, result);

        if (!isDefined)
            throw new ArgumentException($"'{value}' is not a defined value of {enumType}", nameof(value));

        return result;
    }

    private static bool IsNumber(string text) => text.Length > 0 && (char.IsDigit(text[0]) || text[0] == '-');
}
