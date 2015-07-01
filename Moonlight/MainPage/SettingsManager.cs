namespace Moonlight
{
    using Windows.Storage;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// Persist user settings between app uses
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        #region Persistent UI Settings
        /// <summary>
        /// Save page settings so the user doesn't have to select them each time she opens the app
        /// </summary>
        private void SaveSettings()
        {
            var settings = ApplicationData.Current.RoamingSettings;

            // Resolution/FPS box selection
            settings.Values["resolution_fps"] = Resolution_FPS_Box.SelectedIndex;
            settings.Values["full_screen"] = Full_Screen_Checkbox.IsChecked;
            settings.Values["optimize_settings"] = Optimize_Settings_Checkbox.IsChecked;
            settings.Values["pc_audio"] = PC_Audio_Checkbox.IsChecked;
            settings.Values["disable_warnings"] = Disable_Warnings_Checkbox.IsChecked; 
        }

        /// <summary>
        /// Load persisted settings. Called on page load. 
        /// </summary>
        private void LoadSettings()
        {
            var settings = ApplicationData.Current.RoamingSettings;
            // Load fps/resolution combo box
            if (settings.Values.ContainsKey("resolution_fps"))
            {
                Resolution_FPS_Box.SelectedIndex = (int)settings.Values["resolution_fps"];
            }
            // Load checkboxes
            if (settings.Values.ContainsKey("full_screen"))
            {
                Full_Screen_Checkbox.IsChecked = (bool)settings.Values["full_screen"];
            }
            if (settings.Values.ContainsKey("optimize_settings"))
            {
                Optimize_Settings_Checkbox.IsChecked = (bool)settings.Values["optimize_settings"];
            }
            if (settings.Values.ContainsKey("pc_audio"))
            {
                PC_Audio_Checkbox.IsChecked = (bool)settings.Values["pc_audio"];
            }
            if (settings.Values.ContainsKey("disable_warnings"))
            {
                Disable_Warnings_Checkbox.IsChecked = (bool)settings.Values["disable_warnings"];
            }
        }
        #endregion Persistent UI Settings
    }
}