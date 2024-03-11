using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using UMediaPlayer.Models;

namespace UMediaPlayer.Converters
{
    [ValueConversion(typeof(TimeSpan), typeof(double))]
    public class TimeStateToSliderValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var current = (TimeSpan)value;
            var length = MainWindow.GlobalState.Length;
            if (length.TotalMilliseconds == 0)
            {
                return 0;
            }

            return (current.TotalMilliseconds / length.TotalMilliseconds) * 10.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var length = MainWindow.GlobalState.Length;
            return TimeSpan.FromMilliseconds(length.TotalMilliseconds * (double)value / 10);
        }
    }
}
