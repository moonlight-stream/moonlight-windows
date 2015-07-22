namespace Moonlight
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.UI.Core;

    using Moonlight.Streaming;
    using Moonlight.Utils;

    /// <summary>
    /// Performs pairing with the streaming machine
    /// </summary>
    public class PairingManager
    {
        private NvHttp nv;

        /// <summary>
        /// Constructor that sets nv 
        /// </summary>
        /// <param name="nv">The NvHttp Object</param>
        public PairingManager(Computer computer)
        {
            this.nv = new NvHttp(computer.IpAddress); 
        }
        #region Pairing
        /// <summary>
        /// Pair with the hostname in the textbox
        /// </summary>
        public async Task Pair(CoreDispatcher uiDispatcher, Computer c)
        {
            Debug.WriteLine("Pairing...");

            // Get the pair state.
            bool? pairState = await QueryPairState(); 
            if (pairState == true)
            {
                DialogUtils.DisplayDialog(uiDispatcher, "This device is already paired to the host PC", "Already Paired");
                return;
            }
            // pairstate = null. We've encountered an error
            else if (!pairState.HasValue)
            {
                DialogUtils.DisplayDialog(uiDispatcher, "Failed to query pair state", "Pairing failed");
                return;
            }

            bool challenge = await PairingCryptoHelpers.PerformPairingHandshake(uiDispatcher, new WindowsCryptoProvider(), nv, nv.GetUniqueId());
            if (!challenge)
            {
                Debug.WriteLine("Challenges failed");
                return; 
            } 

            // Otherwise, everything was successful
            MainPage.SaveComputer(c);

            // FIXME: We can't have two dialogs open at once.
            // DialogUtils.DisplayDialog(uiDispatcher, "Pairing successful", "Success");
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

            pairState = new XmlQuery(nv.BaseUrl + "/serverinfo?uniqueid=" + nv.GetUniqueId());

            string statusCode = await pairState.ReadXmlRootAttribute("status_code");
            if (statusCode == null)
            {
                return null;
            }

            // Status code 401 means we're not paired
            if (Convert.ToInt32(statusCode) == 401)
            {
                return false;
            }

            string pairStatus = await pairState.ReadXmlElement("PairStatus");
            if (pairStatus == null)
            {
                // Request failed
                return null;
            }

            // Check if the device is paired by checking the XML attribute within the <paired> tag
            if (String.Compare(pairStatus, "1") != 0)
            {
                Debug.WriteLine("Not paired");
                return false;
            }

            // We're already paired if we get here!
            return true;
        }

        #endregion XML Queries
    }
}