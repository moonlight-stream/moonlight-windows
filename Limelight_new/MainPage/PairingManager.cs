namespace Limelight_new
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.Security.Cryptography.Certificates;
    using Windows.Security.Cryptography.Core;
    using Windows.UI.Popups;
    using Windows.UI.Xaml.Controls;
    using Windows.Web.Http.Filters;
    using WindowsRuntime.HttpClientFilters;
    

    /// <summary>
    /// Performs pairing with the streaming machine
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private CryptographicKey aesKey; 

        #region Pairing
        /// <summary>
        /// Pair with the hostname in the textbox
        /// </summary>
        private async Task Pair(string uri)
        {
            Debug.WriteLine("Pairing ");
            // Create NvHttp object with the user input as the URL
            try
            {
                nv = new NvHttp(uri);
            }
            catch (Exception)
            {
                var dialog = new MessageDialog("Invalid Hostname", "Pairing Failed");
                dialog.ShowAsync();
                return;
            }
            // Get the server IP address
            try
            {
                await nv.GetServerIPAddress();
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Error resolving hostname " + e.Message , "Pairing Failed");
                dialog.ShowAsync();
                return;
            }

            // Hit the pairing server. If it fails, return.
            // FIXME real things for PIN
            if (!await PairHelper(nv.GetDeviceName(), "0000"))
            {
                return;
            }

            // Query the app list from the server. If it fails, return
            if (!await QueryAppList())
            {
                return;
            }
            // Otherwise, everything was successful
            var successDialog = new MessageDialog("Pairing successful", "Success");
            await successDialog.ShowAsync();
        }

        /// <summary>
        /// Pair with the server by hitting the pairing URL 
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        private async Task<bool> PairHelper(String uniqueId, String pin)
        {
            // Generate a salt for hashing the PIN
            byte[] salt = GenerateRandomBytes(16);

            // Combine the salt and pin, then create an AES key from them
            byte[] saltAndPin = SaltPin(salt, pin);
            aesKey = GenerateAesKey(saltAndPin);

            // Send the salt and get the server cert

            XmlQuery pairInfo;
            try
            {
                pairInfo = null; 
                // TODO pem
                //pairInfo = new XmlQuery(nv.baseUrl + "/pair?uniqueid=" + uniqueId + 
                //"&devicename=roth&updateState=1&phrase=getservercert&salt=" + bytesToHex(salt) + "&clientcert=" + bytesToHex(pemCertBytes));
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog(e.Message, "Pairing Failed");
                dialog.ShowAsync();
                return false;
            }
            // We aren't paired properly - hit the unpair URL
            if (!pairInfo.XmlAttribute("paired").Equals("0"))
            {
                Unpair(); 
                // TODO get server cert here
            }
            // Everything was successful
            var successDialog = new MessageDialog("Pairing completed successfully", "Pairing Completed");
            await successDialog.ShowAsync();
            return true;
        }
        #endregion Pairing

        #region XML Queries
        /// <summary>
        /// Query the server to get the device pair state
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        private async Task<bool> QueryPairState()
        {
            XmlQuery pairState;
            try
            {
                pairState = new XmlQuery(nv.baseUrl + "/pairstate?uniqueid=" + nv.GetDeviceName());
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Failed to get pair state: " + e.Message);
                dialog.ShowAsync();
                return false;
            }

            // Check if the device is paired by checking the XML attribute within the <paired> tag
            if (String.Compare(pairState.XmlAttribute("paired"), "0") == 0)
            {
                var dialog = new MessageDialog("Device not paired");
                await dialog.ShowAsync();
                return false;
            }
            return true;
        }

        #endregion XML Queries

    }
}