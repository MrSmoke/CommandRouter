namespace CommandRouter.Converters
{
    using System;

    public class SystemTypeConverter : IPropertyConverter
    {
        public bool CanConvert(Type propertyType, object value)
        {
            //TODO: Figure this one out
            return true;
        }

        public object Convert(Type propertyType, object value)
        {
            return System.Convert.ChangeType(value, propertyType);
        }
    }
}
