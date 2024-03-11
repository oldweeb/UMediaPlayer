using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UMediaPlayer.Views
{
    /// <summary>
    /// Interaction logic for JumpWindow.xaml
    /// </summary>
    public partial class JumpWindow : Window
    {
        public JumpWindow()
        {
            InitializeComponent();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            MainWindow.JumpTo.Execute(null);
        }
    }
}
