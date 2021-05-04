using System;
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
            DownloadText.Text = $"{item.Name}\nSubmitted by {item.Owner}";
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = item.EmbedImage;
            bitmap.EndInit();
            Preview.Source = bitmap;
        }
        public DownloadWindow(GameBananaRecord record)
        {
            InitializeComponent();
            DownloadText.Text = $"{record.Title}\nSubmitted by {record.Owner.Name}";
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri($"{record.Media[0].Base}/{record.Media[0].File}");
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
