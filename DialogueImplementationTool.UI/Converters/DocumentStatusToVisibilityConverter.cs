using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DialogueImplementationTool.UI.ViewModels;
namespace DialogueImplementationTool.UI.Converters;

public sealed class DocumentStatusToVisibilityConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not DocumentStatus documentStatus || parameter is not DocumentStatus desiredDocumentStatus) return null;

        return documentStatus == desiredDocumentStatus ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotImplementedException();
    }
}
