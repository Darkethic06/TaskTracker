using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TaskTracker.Models;

namespace TaskTracker.Converters;

/// <summary>true → Visible; false → Collapsed. Set Invert=True to flip.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is bool v && v;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Used to bind RadioButton.IsChecked to a string/enum property.
/// IsChecked = (value.ToString() == parameter); when checked, writes parameter back.
/// </summary>
public class EqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not true || parameter is null) return Binding.DoNothing;
        var p = parameter.ToString()!;
        return targetType.IsEnum ? Enum.Parse(targetType, p) : p;
    }
}

/// <summary>Converts a 0–100 percent into a pixel length, given the full length as parameter.</summary>
public class PercentToLengthConverter : IMultiValueConverter
{
    // values[0] = percent, values[1] = available width
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not double pct || values[1] is not double width)
            return 0.0;
        return Math.Max(0, Math.Min(width, width * pct / 100.0));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
