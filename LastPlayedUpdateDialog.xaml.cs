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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PlayLogger
{
    /// <summary>
    /// Interaction logic for LastPlayedUpdateDialog.xaml
    /// </summary>
    public partial class LastPlayedUpdateDialog : Window
    {
        public LastPlayedUpdateDialog(IEnumerable<string> items)
        {
            InitializeComponent();
            cb.ItemsSource = items.ToDictionary(i => i, i => (object)i);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Places = new HashSet<string>(cb.SelectedItems.Keys);
            this.DialogResult = true;
        }

        public HashSet<string> Places { get; private set; }
    }
}
