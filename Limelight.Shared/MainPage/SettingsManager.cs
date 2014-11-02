namespace Limelight
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
        }

        /// <summary>
        /// Load persisted settings. Called on page load. 
        /// </summary>
        private void LoadSettings()
        {
            var settings = ApplicationData.Current.RoamingSettings;
            // Load fps radio button state
            if (settings.Values.ContainsKey("resolution_fps"))
            {
                Resolution_FPS_Box.SelectedIndex = (int)settings.Values["resolution_fps"];
            }
        }
        #endregion Persistent UI Settings
    }
}