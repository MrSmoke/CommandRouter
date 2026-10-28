namespace CommandRouter.Converters
{
    using System;

    public interface IPropertyConverter
    {
        bool CanConvert(Type propertyType, string value);
        object Convert(Type propertyType, string value);
    }
}