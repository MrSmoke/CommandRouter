namespace CommandRouter.Converters
{
    using System;

    public interface IPropertyConverter
    {
        bool CanConvert(Type propertyType, object value);
        object Convert(Type propertyType, object value);
    }
}