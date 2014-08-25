namespace Limelight
{
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.X509;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using Windows.Security.Cryptography.Certificates;
    using Windows.Storage;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// Persist user settings between app uses
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Persistent UI Settings
        /// <summary>
        /// Save page settings so the user doesn't have to select them each time she opens the app
        /// </summary>
        private void SaveSettings()
        {
            var settings = ApplicationData.Current.RoamingSettings;

            // Save fps radio button state

            settings.Values["fps"] = _30fps_button.IsChecked;
            settings.Values["pixels"] = _720p_button.IsChecked;
        }

        /// <summary>
        /// Load persisted settings. Called on page load. 
        /// </summary>
        private void LoadSettings()
        {
            var settings = ApplicationData.Current.RoamingSettings;
            // Load fps radio button state
            if (settings.Values.ContainsKey("fps"))
            {
                if ((bool)settings.Values["fps"])
                {
                    _30fps_button.IsChecked = true;
                }
                else
                {
                    _60fps_button.IsChecked = true;
                }
            }
            // Load fps radio button state
            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("pixels"))
            {
                if ((bool)settings.Values["pixels"])
                {
                    _720p_button.IsChecked = true;
                }
                else
                {
                    _1080p_button.IsChecked = true;
                }
            }
        }
        #endregion Persistent UI Settings

        #region Persistent paired computer list
        
        /// <summary>
        /// Once we freshly pair to a computer, save it
        /// </summary>
        /// <param name="c">Computer we've paired to</param>
        public static void SaveComputer(Computer c)
        {
            var settings = ApplicationData.Current.RoamingSettings;
            settings.Values["computerName"] = c.Name;
            settings.Values["computerIP"] = c.IpAddress;
        }

        /// <summary>
        /// Load the last computer we've paired to
        /// </summary>
        /// <returns>Last computer we've paired to or null if none</returns>
        public static Computer LoadComputer()
        {
            var settings = ApplicationData.Current.RoamingSettings;

            if (!settings.Values.ContainsKey("computerName") || !settings.Values.ContainsKey("computerIP"))
            {
                return null;
            }
            string name = settings.Values["computerName"] as string;
            string ip = settings.Values["computerIP"] as string;
            return new Computer(name, ip);
        }

        #endregion Persistent paired computer list
    }
}