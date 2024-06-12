using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DialogueImplementationTool.Services;
namespace DialogueImplementationTool.UI.Converters;

public class LoadStateToBrushConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not LoadState loadState) return null;

        return loadState switch {
            LoadState.NotLoaded => Brushes.IndianRed,
            LoadState.InProgress => Brushes.CornflowerBlue,
            LoadState.Loaded => Brushes.ForestGreen,
            _ => throw new InvalidOperationException(),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}
