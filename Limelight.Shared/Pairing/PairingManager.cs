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
    public partial class Pairing
    {
        private CryptographicKey aesKey;
        private NvHttp nv;

        /// <summary>
        /// Constructor that sets nv 
        /// </summary>
        /// <param name="nv">The NvHttp Object</param>
        public Pairing(NvHttp nv)
        {
            this.nv = nv; 
        }
        #region Pairing
        /// <summary>
        /// Pair with the hostname in the textbox
        /// </summary>
        public async Task Pair(Computer c)
        {
            Debug.WriteLine("Pairing ");
            // Create NvHttp object with the user input as the URL
            try
            {
                nv = new NvHttp(c.IpAddress);
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
                await nv.ServerIPAddress();
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Error resolving hostname " + e.Message , "Pairing Failed");
                dialog.ShowAsync();
                return;
            }

            // "Please don't do this ever, but it's only okay because Cameron said so" -Cameron Gutman            
            getClientCertificate();

            // Get the pair state.
            bool? pairState = await QueryPairState(); 
            if (pairState == true)
            {
                var dialog = new MessageDialog("This device is already paired to the host PC", "Already Paired");
                dialog.ShowAsync();
                Debug.WriteLine("Already paired");
                return;
            }
                // pairstate = null. We've encountered an error
            else if (!pairState.HasValue)
            {
                var dialog = new MessageDialog("Failed to query pair state", "Pairing failed");
                dialog.ShowAsync();
                Debug.WriteLine("Query pair state failed");
                return;
            }
            bool challenge = await Challenges(nv.GetUniqueId());
            if (!challenge)
            {
                Debug.WriteLine("Challenges failed");
                return; 
            } 

            // Otherwise, everything was successful
            MainPage.SaveComputer(c);
            var successDialog = new MessageDialog("Pairing successful", "Success");
            await successDialog.ShowAsync();
        }

        #endregion Pairing

        #region XML Queries
        /// <summary>
        /// Query the server to get the device pair state
        /// </summary>
        /// <returns>True if device is already paired, false if not, null if failure</returns>
        public async Task<bool?> QueryPairState()
        {
            XmlQuery pairState;
            try
            {
                pairState = new XmlQuery(nv.baseUrl + "/serverinfo?uniqueid=" + nv.GetUniqueId());
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to get pair state: " + e.Message);
                
                return null;
            }

            // Check if the device is paired by checking the XML attribute within the <paired> tag
            if (String.Compare(pairState.XmlAttribute("PairStatus"), "1") != 0)
            {
                Debug.WriteLine("Not paired");
                return false;
            }
            // We're already paired if we get here!
            return true;
        }

        /// <summary>
        /// Getter for the NvHttp object
        /// </summary>
        /// <returns>nv</returns>
        public NvHttp getNv() {
            return nv; 
        }

        #endregion XML Queries
    }
}