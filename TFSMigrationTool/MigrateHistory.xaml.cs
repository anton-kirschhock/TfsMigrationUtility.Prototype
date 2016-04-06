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

namespace TFSMigrationTool
{
    /// <summary>
    /// Interaction logic for Migrate.xaml
    /// </summary>
    public partial class MigrateHistory : Window
    {
        public MigrateHistory()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox t = sender as TextBox;
            t.Focus();
            t.CaretIndex = t.Text.Length;
            t.ScrollToEnd();
        }
    }
}
