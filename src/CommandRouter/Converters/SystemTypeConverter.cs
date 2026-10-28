namespace CommandRouter.Converters
{
    using System;
    using System.Globalization;

    public class SystemTypeConverter : IPropertyConverter
    {
        public bool CanConvert(Type propertyType, string value)
        {
            var target = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (target == typeof(string) || target.IsEnum || target == typeof(Guid) ||
                target == typeof(TimeSpan) || target == typeof(DateTimeOffset))
                return true;

            return Type.GetTypeCode(target) is not (TypeCode.Object or TypeCode.Empty or TypeCode.DBNull);
        }

        public object Convert(Type propertyType, string value)
        {
            var target = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (target == typeof(string))
                return value;

            if (target.IsEnum)
                return Enum.Parse(target, value, true);

            if (target == typeof(Guid))
                return Guid.Parse(value);

            if (target == typeof(TimeSpan))
                return TimeSpan.Parse(value, CultureInfo.InvariantCulture);

            if (target == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);

            return System.Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }
    }
}
