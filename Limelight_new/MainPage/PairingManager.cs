namespace Limelight_new
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.UI.Popups;
    using Windows.UI.Xaml.Controls;
    using Windows.Security.Cryptography;
    using Windows.Storage.Streams;
    using Windows.Security.Cryptography.Core;
    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.Security;
    using Org.BouncyCastle.Utilities.Encoders;
    using System.Text;
    using Org.BouncyCastle.Crypto.Paddings;

    /// <summary>
    /// Performs pairing with the streaming machine
    /// </summary>
    public sealed partial class MainPage : Page
    {
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
            if (!await PairHelper())
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
        private async Task<bool> PairHelper()
        {
            // Making the XML query to this URL does the actual pairing
            XmlQuery pairInfo;
            try
            {
                pairInfo = new XmlQuery(nv.baseUrl + "/pair?uniqueid=" + nv.GetDeviceName());
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog(e.Message, "Pairing Failed");
                dialog.ShowAsync();
                return false;
            }
            // Session ID = 0; pairing failed
            if (String.Compare(pairInfo.XmlAttribute("sessionid"), "0") == 0)
            {
                var dialog = new MessageDialog("Session ID = 0", "Pairing Failed");
                await dialog.ShowAsync();
                return false;
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