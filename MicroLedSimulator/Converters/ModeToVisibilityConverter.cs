// Converters/ModeToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using MicroLedSimulator.Models; // 假設 CameraMode 在這裡

namespace MicroLedSimulator.Converters
{
    public class ModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CameraMode currentMode && parameter is string targetModeString)
            {
                if (Enum.TryParse<CameraMode>(targetModeString, true, out CameraMode targetMode))
                {
                    return currentMode == targetMode ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}