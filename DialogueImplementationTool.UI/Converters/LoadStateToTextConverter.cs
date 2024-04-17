using System.Globalization;
using System.Windows.Data;
using DialogueImplementationTool.UI.ViewModels;
namespace DialogueImplementationTool.UI.Converters;

public class LoadStateToTextConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not LoadState loadState) return null;

        return loadState switch {
            LoadState.NotLoaded => "Not Loaded",
            LoadState.InProgress => "Loading",
            LoadState.Loaded => "Ready",
            _ => throw new InvalidOperationException()
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}