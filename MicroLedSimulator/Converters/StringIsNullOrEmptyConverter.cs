using System;
using System.Globalization;
using System.Windows.Data;

namespace MicroLedSimulator.Converters
{
    [ValueConversion(typeof(string), typeof(bool))]
    public class StringIsNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not used in a one-way binding scenario like this,
            // but it's good practice to implement it if the converter could be used two-way.
            // For this specific case, it's unlikely to be used.
            throw new NotImplementedException();
        }
    }
}
