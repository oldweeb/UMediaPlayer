using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace UMediaPlayer.Extensions
{
    public static class WpfExtensions
    {
        public static T GetTemplatedParent<T>(this FrameworkElement element) where T : DependencyObject
        {
            DependencyObject child = element, parent = null;

            while (child != null && (parent = LogicalTreeHelper.GetParent(child)) == null)
            {
                child = VisualTreeHelper.GetParent(child);
            }

            FrameworkElement frameworkParent = parent as FrameworkElement;

            return frameworkParent?.TemplatedParent as T;
        }
    }
}
