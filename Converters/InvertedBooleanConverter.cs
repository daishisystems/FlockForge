using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace FlockForge.Converters
{
    public class InvertedBooleanConverter : IValueConverter
    {
        public InvertedBooleanConverter()
        {
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}
