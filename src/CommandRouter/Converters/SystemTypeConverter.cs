namespace CommandRouter.Converters;

using System;
using System.Globalization;

public class SystemTypeConverter : IPropertyConverter
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

        // Combined flags aren't individually defined
        if (!enumType.IsDefined(typeof(FlagsAttribute), false) && !Enum.IsDefined(enumType, result))
            throw new ArgumentException($"'{value}' is not a defined value of {enumType}", nameof(value));

        return result;
    }
}
