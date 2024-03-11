using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Vlc.DotNet.Core;

namespace UMediaPlayer.Converters
{
    [ValueConversion(typeof(List<TrackDescription>), typeof(IEnumerable<string>))]
    public class TrackToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return "";
            }

            var tracks = (List<TrackDescription>)value;
            return tracks.Select(t => t.Name);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
