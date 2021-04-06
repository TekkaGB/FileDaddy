using Microsoft.Win32;
using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;

namespace FNF_Mod_Manager.UI
{
    /// <summary>
    /// Interaction logic for ChangelogBox.xaml
    /// </summary>
    public partial class ChangelogBox : Window
    {
        public bool YesNo = false;

        // An instance where you can't skip the update (only yes or no)
        public ChangelogBox(GameBananaItemUpdate update, string packageName, string text)
        {
            InitializeComponent();
            ChangesGrid.ItemsSource = update.Changes;
            Title = $"{packageName} Changelog";
            VersionLabel.Content = update.Title;
            Text.Text = text;
            Grid.SetColumnSpan(YesButton, 2);
            Grid.SetColumnSpan(NoButton, 2);
            PlayNotificationSound();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            YesNo = true;
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
