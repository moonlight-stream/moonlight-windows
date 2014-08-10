namespace Limelight
{
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.X509;
    using System.IO;
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
            // Load fps radio button state
            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("fps"))
            {
                if ((bool)ApplicationData.Current.RoamingSettings.Values["fps"])
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
                if ((bool)ApplicationData.Current.RoamingSettings.Values["pixels"])
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

        #region Persistent Crypto Settings
        private void SaveCertKeyPair()
        {
            var settings = ApplicationData.Current.RoamingSettings;
            PemWriter certWriter = new PemWriter(new StringWriter());
            PemWriter keyWriter = new PemWriter(new StringWriter());

            certWriter.WriteObject(cert);
            certWriter.Writer.Flush();

            keyWriter.WriteObject(keyPair);
            keyWriter.Writer.Flush(); 

            // Line endings MUST be UNIX for the PC to accept the cert properly
            string keyStr = keyWriter.Writer.ToString();
            keyStr = keyStr.Replace("\r\n", "\n");

            string certStr = certWriter.Writer.ToString(); 
            certStr = certStr.Replace("\r\n", "\n");

            settings.Values["cert"] = certStr;
            settings.Values["key"] = keyStr; 
        }

        /// <summary>
        /// Load the cert/key pair from memory
        /// </summary>
        /// <returns>Value indicating success</returns>
        private bool LoadCertKeyPair()
        {
            var settings = ApplicationData.Current.RoamingSettings;
            if (settings.Values.ContainsKey("cert") && settings.Values.ContainsKey("key"))
            {
                string certStr = ApplicationData.Current.RoamingSettings.Values["cert"].ToString();
                string keyStr = ApplicationData.Current.RoamingSettings.Values["key"].ToString();

                PemReader certReader = new PemReader(new StringReader(certStr));
                PemReader keyReader = new PemReader(new StringReader(keyStr));

                keyPair = (AsymmetricCipherKeyPair)keyReader.ReadObject();
                cert = (X509Certificate)certReader.ReadObject();
                pemCertBytes = System.Text.Encoding.UTF8.GetBytes(certStr);

                return true; 
            }
             // We didn't load the cert/key pair properly
            else
            {
                return false; 
            }
        }
        #endregion Persistent Crypto Settings
    }
}