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
using Microsoft.Win32;
using System.Media;

namespace FNF_Mod_Manager.UI
{
    /// <summary>
    /// Interaction logic for UpdateFileBox.xaml
    /// </summary>
    public partial class UpdateFileBox : Window
    {
        public string chosenFileUrl;
        public string chosenFileName;
        private bool list;
        public UpdateFileBox(Dictionary<String, GameBananaItemFile> files, string packageName)
        {
            InitializeComponent();
            FileGrid.ItemsSource = files;
            FileGrid.SelectedIndex = 0;
            NameColumn.Binding = new Binding("Value.FileName");
            UploadTimeColumn.Binding = new Binding("Value.TimeSinceUpload");
            DescriptionColumn.Binding = new Binding("Value.Description");
            SizeColumn.Binding = new Binding("Value.ConvertedFileSize");
            Title = $"FileDaddy - {packageName}";
        }
        public UpdateFileBox(List<GameBananaItemFile> files, string packageName)
        {
            InitializeComponent();
            FileGrid.ItemsSource = files;
            if (!files.Select(x => x.Description).Where(y => y.Length > 0).Any())
                DescriptionColumn.Visibility = Visibility.Collapsed;
            FileGrid.SelectedIndex = 0;
            NameColumn.Binding = new Binding("FileName");
            UploadTimeColumn.Binding = new Binding("TimeSinceUpload");
            DescriptionColumn.Binding = new Binding("Description");
            SizeColumn.Binding = new Binding("ConvertedFileSize");
            Title = $"FileDaddy - {packageName}";
            list = true;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (list)
            {
                GameBananaItemFile selectedItem = (GameBananaItemFile)FileGrid.SelectedItem;
                chosenFileUrl = selectedItem.DownloadUrl;
                chosenFileName = selectedItem.FileName;
            }
            else
            {
                KeyValuePair<String, GameBananaItemFile> selectedItem = (KeyValuePair<String, GameBananaItemFile>)FileGrid.SelectedItem;
                chosenFileUrl = selectedItem.Value.DownloadUrl;
                chosenFileName = selectedItem.Value.FileName;
            }
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
