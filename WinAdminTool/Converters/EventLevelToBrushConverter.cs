using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinAdminTool.Converters
{
    public class EventLevelToBrushConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            string language)
        {
            string level = value?.ToString() ?? string.Empty;

            return level switch
            {
                "Kritisch" => new SolidColorBrush(
                    Color.FromArgb(255, 220, 60, 60)),

                "Fehler" => new SolidColorBrush(
                    Color.FromArgb(255, 230, 80, 80)),

                "Warnung" => new SolidColorBrush(
                    Color.FromArgb(255, 230, 180, 50)),

                "Information" => new SolidColorBrush(
                    Color.FromArgb(255, 70, 140, 230)),

                "Ausführlich" => new SolidColorBrush(
                    Color.FromArgb(255, 130, 130, 130)),

                _ => new SolidColorBrush(
                    Color.FromArgb(255, 150, 150, 150))
            };
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