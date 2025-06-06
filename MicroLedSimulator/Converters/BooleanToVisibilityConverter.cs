using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MicroLedSimulator.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if (value is bool bValue) // 直接模式匹配 bool
            {
                flag = bValue;
            }
            // 不需要特別處理 bool?，因為如果 value 是 bool? 且有值，
            // is bool 會正確處理。如果它是 null，則 flag 保持 false。
            // 或者，如果希望 null 也被視為 false：
            else if (value == null)
            {
                flag = false;
            }
            // 如果你確實需要區分 bool? 的 null 和 false，可以這樣：
            // else if (value is bool? nullableValue)
            // {
            //    flag = nullableValue.HasValue ? nullableValue.Value : false; // 如果是 null，視為 false
            // }


            if (parameter != null && parameter.ToString() == "Invert")
            {
                flag = !flag;
            }

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}