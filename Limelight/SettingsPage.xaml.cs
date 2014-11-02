using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Limelight
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            LoadSettings();

#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtonsBackPressed;
#endif
        }

        #region EventHandlers
#if WINDOWS_PHONE_APP
        /// <summary>
        /// If Windows Phone, go backwards instead of quitting the app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HardwareButtonsBackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            SaveSettings(); 
            e.Handled = true;
            Frame.GoBack();
        }
#endif
        /// <summary>
        /// Save settings when one navigates away from the page
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            SaveSettings();
        }

        /// <summary>
        /// Save settings and return to the main page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            Frame.GoBack();
        }

        #endregion EventHandlers

        #region Stream Settings
        /// <summary>
        /// Get the width of the stream from the setting choice
        /// </summary>
        /// <returns></returns>
        internal int GetStreamWidth()
        {
            if (Resolution_FPS_Box.SelectedIndex == 0 || Resolution_FPS_Box.SelectedIndex == 1)
            {
                return 1280;
            }
            else
            {
                return 1920;
            }
        }

        /// <summary>
        /// Get height of the stream from the setting
        /// </summary>
        /// <returns>Stream height in pixels</returns>
        internal int GetStreamHeight()
        {
            if (Resolution_FPS_Box.SelectedIndex == 0 || Resolution_FPS_Box.SelectedIndex == 1)
            {
                return 720;
            }
            else
            {
                return 1080;
            }
        }

        /// <summary>
        /// Get Frames per Second from the setting
        /// </summary>
        /// <returns></returns>
        internal int GetStreamFps()
        {
            if (Resolution_FPS_Box.SelectedIndex == 1 || Resolution_FPS_Box.SelectedIndex == 3)
            {
                return 60;
            }
            else
            {
                return 30;
            }
        }
        #endregion Stream Settings
    }
}
