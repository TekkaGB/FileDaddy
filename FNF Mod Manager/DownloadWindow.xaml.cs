using System.Windows;

namespace FNF_Mod_Manager
{
    /// <summary>
    /// Interaction logic for Download.xaml
    /// </summary>
    public partial class DownloadWindow : Window
    {
        public bool YesNo = false;
        public DownloadWindow(string name)
        {
            InitializeComponent();
            DownloadText.Text = $"Would you like to download {name}?";
            
        }
        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            YesNo = true;
            
            Close();
        }
        private void No_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
