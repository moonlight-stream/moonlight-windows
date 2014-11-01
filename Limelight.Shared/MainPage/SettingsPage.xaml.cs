using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

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

        #endregion EventHandlers

        #region Stream Settings
        /// <summary>
        /// Get the width of the stream from the setting choice
        /// </summary>
        /// <returns></returns>
        internal int GetStreamWidth()
        {
            if (_720p_button.IsChecked.Value)
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
            if (_720p_button.IsChecked.Value)
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
            if (_60fps_button.IsChecked.Value)
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
