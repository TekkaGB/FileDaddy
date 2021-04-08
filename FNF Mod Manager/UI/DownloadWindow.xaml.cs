using System.Windows;
using System.Windows.Media.Imaging;

namespace FNF_Mod_Manager
{
    /// <summary>
    /// Interaction logic for Download.xaml
    /// </summary>
    public partial class DownloadWindow : Window
    {
        public bool YesNo = false;
        public DownloadWindow(GameBananaItem item)
        {
            InitializeComponent();
            DownloadText.Text = $"Would you like to download {item.Name}?";
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = item.SubFeedImage;
            bitmap.EndInit();
            Preview.Source = bitmap;
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
