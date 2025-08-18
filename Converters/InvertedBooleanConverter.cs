using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace FlockForge.Converters;

public sealed class InvertedBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
