namespace CommandRouter.Tests.TypeConversion;

using System;
using System.Globalization;
using CommandRouter.Converters;
using Xunit;

public class SystemTypeConverterTests
{
    private readonly SystemTypeConverter _converter = new();

    [Fact]
    public void Convert_NullableInt_ReturnsInt()
    {
        Assert.True(_converter.CanConvert(typeof(int?), "5"));
        Assert.Equal(5, _converter.Convert(typeof(int?), "5"));
    }

    [Fact]
    public void Convert_Enum_ParsesNameIgnoringCase()
    {
        Assert.True(_converter.CanConvert(typeof(DayOfWeek), "friday"));
        Assert.Equal(DayOfWeek.Friday, _converter.Convert(typeof(DayOfWeek), "friday"));
    }

    [Fact]
    public void Convert_NullableEnum_ParsesName()
    {
        Assert.Equal(DayOfWeek.Monday, _converter.Convert(typeof(DayOfWeek?), "Monday"));
    }

    [Fact]
    public void Convert_Guid_ParsesGuid()
    {
        var guid = Guid.NewGuid();

        Assert.True(_converter.CanConvert(typeof(Guid), guid.ToString()));
        Assert.Equal(guid, _converter.Convert(typeof(Guid), guid.ToString()));
    }

    [Fact]
    public void Convert_TimeSpan_ParsesTimeSpan()
    {
        Assert.True(_converter.CanConvert(typeof(TimeSpan), "01:02:03"));
        Assert.Equal(new TimeSpan(1, 2, 3), _converter.Convert(typeof(TimeSpan), "01:02:03"));
    }

    [Fact]
    public void Convert_DateTimeOffset_ParsesDateTimeOffset()
    {
        Assert.True(_converter.CanConvert(typeof(DateTimeOffset), "2020-01-02T03:04:05+00:00"));
        Assert.Equal(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero),
            _converter.Convert(typeof(DateTimeOffset), "2020-01-02T03:04:05+00:00"));
    }

    [Fact]
    public void Convert_Double_UsesInvariantCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal(1.5d, _converter.Convert(typeof(double), "1.5"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Convert_String_ReturnsSameString()
    {
        Assert.True(_converter.CanConvert(typeof(string), "abc"));
        Assert.Equal("abc", _converter.Convert(typeof(string), "abc"));
    }

    [Fact]
    public void CanConvert_UnsupportedType_ReturnsFalse()
    {
        Assert.False(_converter.CanConvert(typeof(UnsupportedType), "abc"));
    }

    private sealed class UnsupportedType
    {
    }
}