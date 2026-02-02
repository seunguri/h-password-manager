using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PasswordProtector.Models;

namespace PasswordProtector.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Account account)
            {
                return account.StatusColor;
            }
            
            // Default to green
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4EC9B0"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
