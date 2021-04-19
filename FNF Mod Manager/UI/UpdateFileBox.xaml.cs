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
        private readonly string host;
        // GameBanana Files
        public UpdateFileBox(Dictionary<String, GameBananaItemFile> files, string packageName)
        {
            InitializeComponent();
            FileGrid.ItemsSource = files;
            FileGrid.SelectedIndex = 0;
            NameColumn.Binding = new Binding("Value.FileName");
            UploadTimeColumn.Binding = new Binding("Value.TimeSinceUpload");
            DescriptionColumn.Binding = new Binding("Value.Description");
            Title = $"FileDaddy - {packageName}";
            host = "GameBanana";
            PlayNotificationSound();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            KeyValuePair<String, GameBananaItemFile> selectedItem = (KeyValuePair<String, GameBananaItemFile>)FileGrid.SelectedItem;
            chosenFileUrl = selectedItem.Value.DownloadUrl;
            chosenFileName = selectedItem.Value.FileName;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void PlayNotificationSound()
        {
            bool found = false;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue(null); // pass null to get (Default)
                        if (o != null)
                        {
                            SoundPlayer theSound = new SoundPlayer((String)o);
                            theSound.Play();
                            found = true;
                        }
                    }
                }
            }
            catch
            { }
            if (!found)
                SystemSounds.Beep.Play(); // consolation prize
        }
    }
}
