using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PlayLogger
{
    public partial class SettingsPopup : System.Windows.Controls.UserControl
    {
        public SettingsPopup()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    LastPlayDirTB.Text = dialog.SelectedPath;
                }
            }
        }

        private void logoBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dir = System.IO.Path.GetDirectoryName(logoTB.Text);
            using (var dialog = new OpenFileDialog() { InitialDirectory = dir })
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    logoTB.Text = dialog.FileName;
                }
            }
        }
    }
}
