using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PasswordProtector.Converters
{
    /// <summary>
    /// 태그 이름을 기준으로 일관된 강조색을 반환합니다.
    /// 같은 태그는 다시 열어도 같은 색으로 보여 태그를 빠르게 구분할 수 있습니다.
    /// </summary>
    public class TagColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush[] Palette =
        {
            CreateBrush("#2457A6"), // blue
            CreateBrush("#6D3EAF"), // purple
            CreateBrush("#0F766E"), // teal
            CreateBrush("#A34A1C"), // orange
            CreateBrush("#A12B52"), // rose
            CreateBrush("#4964B8"), // indigo
            CreateBrush("#277A61"), // green
            CreateBrush("#8B4F1E")  // amber
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tag = value as string;
            if (string.IsNullOrWhiteSpace(tag))
                return Palette[0];

            unchecked
            {
                // string.GetHashCode()는 실행마다 달라질 수 있어, 색이 유지되는 간단한 해시를 사용합니다.
                var hash = 17;
                foreach (var character in tag.Trim())
                    hash = hash * 31 + character;

                return Palette[(hash & 0x7fffffff) % Palette.Length];
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();

        private static SolidColorBrush CreateBrush(string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze();
            return brush;
        }
    }
}
