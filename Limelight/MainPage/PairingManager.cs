namespace Limelight
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.Security.Cryptography.Certificates;
    using Windows.Security.Cryptography.Core;
    using Windows.UI.Popups;
    using Windows.UI.Xaml.Controls;
    using Windows.Web.Http.Filters;

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
            // "Please don't do this ever, but it's only okay because Cameron said so" -Cameron Gutman
            getClientCertificate(); 

            if (await QueryPairState())
            {
                var dialog = new MessageDialog("This device is already paired to the host PC", "Already Paired");
                dialog.ShowAsync();
                Debug.WriteLine("Already paired");
                return;
            }

            if (!Challenges(nv.GetDeviceName()))
            {
                Debug.WriteLine("Challenges failed");
                return; 
            } 

            // Otherwise, everything was successful
            var successDialog = new MessageDialog("Pairing successful", "Success");
            await successDialog.ShowAsync();
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
                pairState = new XmlQuery(nv.baseUrl + "/serverinfo?uniqueid=" + nv.GetDeviceName());
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Failed to get pair state: " + e.Message);
                dialog.ShowAsync();
                return false;
            }

            // Check if the device is paired by checking the XML attribute within the <paired> tag
            if (String.Compare(pairState.XmlAttribute("PairStatus"), "1") != 0)
            {
                Debug.WriteLine("Not paired");
                return false;
            }
            return true;
        }
        #endregion XML Queries
    }
}