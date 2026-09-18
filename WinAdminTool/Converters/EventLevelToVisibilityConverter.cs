using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WinAdminTool.Converters
{
    public class EventLevelToVisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            string language)
        {
            string level = value?.ToString() ?? string.Empty;
            string requestedLevel = parameter?.ToString() ?? string.Empty;

            bool isCritical = string.Equals(
                level,
                "Kritisch",
                StringComparison.OrdinalIgnoreCase);

            if (string.Equals(
                    requestedLevel,
                    "Critical",
                    StringComparison.OrdinalIgnoreCase))
            {
                return isCritical
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            if (string.Equals(
                    requestedLevel,
                    "Normal",
                    StringComparison.OrdinalIgnoreCase))
            {
                return isCritical
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            string language)
        {
            throw new NotSupportedException();
        }
    }
}