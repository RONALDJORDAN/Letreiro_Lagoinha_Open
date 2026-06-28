using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LetreiroDigital.Converters
{
    public class EqualityToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] != null && values[1] != null)
            {
                if (values[0].ToString() == values[1].ToString())
                {
                    // Selected Color (Green)
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                }
            }
            // Default Color (Dark Gray)
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
