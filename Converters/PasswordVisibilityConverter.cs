using System;
using System.Globalization;
using System.Windows.Data;

namespace PasswordProtector.Converters
{
    public class PasswordVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is string password && values[1] is bool isVisible)
            {
                return isVisible ? password : new string('•', password?.Length ?? 0);
            }
            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
