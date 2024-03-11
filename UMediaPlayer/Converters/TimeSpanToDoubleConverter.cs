﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace UMediaPlayer.Converters
{
    [ValueConversion(typeof(TimeSpan), typeof(double))]
    public class TimeSpanToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((TimeSpan)value).TotalMilliseconds;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TimeSpan.FromMilliseconds((double)value);
        }
    }
}
