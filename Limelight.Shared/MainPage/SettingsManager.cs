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
        private static List<Computer> pairedComputers;
        #region Persistent UI Settings
        // TODO Save paired PCs
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

        public static void SaveComputer(Computer c)
        {
            pairedComputers = LoadComputers();
            pairedComputers.Add(c);

            // Save the updated list for future use
            var settings = ApplicationData.Current.RoamingSettings;
            settings.Values["computers"] = pairedComputers; 
        }

        /// <summary>
        /// Load the list of saved computers from memory
        /// </summary>
        /// <returns>The list of computers that we've previously paired to</returns>
        public static List<Computer> LoadComputers()
        {
            var settings = ApplicationData.Current.RoamingSettings;

            if (!settings.Values.ContainsKey("computers"))
            {
                return new List<Computer>();
            }
            else
            {
                return settings.Values["computers"] as List<Computer>;
            }
        }

        // TODO allow the user to delete a computer

        #endregion Persistent paired computer list
    }
}