using System.Globalization;
using System.Windows.Data;
namespace DialogueImplementationTool.UI.Converters;

public sealed class IntPlusOneConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is int i) return i + 1;

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is int i) return i - 1;

        return null;
    }
}
