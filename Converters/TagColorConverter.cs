using System;
using System.Globalization;
using System.Windows;
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
        private static readonly string[] PaletteKeys =
        {
            "Tag1Brush", "Tag2Brush", "Tag3Brush", "Tag4Brush",
            "Tag5Brush", "Tag6Brush", "Tag7Brush", "Tag8Brush"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tag = value as string;
            if (string.IsNullOrWhiteSpace(tag))
                return GetThemeBrush(0);

            unchecked
            {
                // string.GetHashCode()는 실행마다 달라질 수 있어, 색이 유지되는 간단한 해시를 사용합니다.
                var hash = 17;
                foreach (var character in tag.Trim())
                    hash = hash * 31 + character;

                return GetThemeBrush((hash & 0x7fffffff) % PaletteKeys.Length);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();

        private static SolidColorBrush GetThemeBrush(int index) =>
            Application.Current?.TryFindResource(PaletteKeys[index]) as SolidColorBrush ?? Brushes.SlateGray;
    }
}
