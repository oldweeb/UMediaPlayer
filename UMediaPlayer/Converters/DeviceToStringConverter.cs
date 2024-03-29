﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Vlc.DotNet.Core.Interops;

namespace UMediaPlayer.Converters
{
    [ValueConversion(typeof(List<AudioOutputDevice>), typeof(IEnumerable<string>))]
    public class DeviceToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return null;
            var devices = (List<AudioOutputDevice>)value;
            return devices.Select(d => d.Description);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
