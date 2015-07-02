using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.System.Profile;

namespace Moonlight
{    
    /// <summary>
    /// Object that obtains the base URL for http requests
    /// </summary>
    public class NvHttp
    {
        #region Class Variables

        public const int PORT = 47984; 
	    public const int CONNECTION_TIMEOUT = 5000;
        // TODO try internal, then try external IP address
        public string BaseUrl { get; set; }
        private string hostname;
        private Regex IP = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");

        #endregion Class Variables

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="NvHttp"/> class. 
        /// </summary>
        /// <param name="hostnameString">Hostname or IP address of the streaming machine</param>
        public NvHttp(string hostnameString)
        {
            this.hostname = hostnameString;
            this.BaseUrl = "https://" + hostname + ":" + PORT; 
        }
        #endregion Constructor

        #region Getters
        /// Get the local device's name
        /// </summary>
        /// <returns>Unique ID for the device</returns>
        public String GetUniqueId()
        {
            var settings = ApplicationData.Current.RoamingSettings;
            byte[] bytes;

            if (settings.Values.ContainsKey("uniqueid"))
            {
                bytes = (byte[])settings.Values["uniqueid"];
            }
            else
            {
                bytes = PairingCryptoHelpers.GenerateRandomBytes(8);
                settings.Values["uniqueid"] = bytes;
            }

            return PairingCryptoHelpers.BytesToHex(bytes);
        }

        /// <summary>
        /// Finds the IP address of the streaming machine
        /// </summary>
        public async Task<String> ResolveServerIPAddress()
        {
            Match ipAddr = null;

            // Check if it's an IP address as-is based on a regex match
            try
            {
                ipAddr = IP.Match(this.hostname);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
            }

            // If the regex matched, we already have the IP string we need.
            if (ipAddr.Success)
            {
                return hostname;
            }
            // Else, we need to resolve the hostname. 
            else
            {
                return await ResolveHostName(this.hostname);
            }
        }
        #endregion Getters

        #region Hostname resolution

        /// <summary>
        /// Resolve the GEForce PC hostname to an IP Address
        /// </summary>
        /// <param name="hostName">Hostname to resolve</param>
        private async Task<String> ResolveHostName(String hostName)
        {
            HostName serverHost = new HostName(hostName);
            StreamSocket clientSocket = new Windows.Networking.Sockets.StreamSocket();

            // Try to connect to the remote host
            try
            {
                await clientSocket.ConnectAsync(serverHost, "47984");
            }
            catch (Exception e)
            {
                Debug.WriteLine("ResolveHostName Exception: " + e.Message);
                return null;
            }

            return clientSocket.Information.RemoteAddress.ToString();           
        }
        #endregion Hostname resolution

        #region Helpers

        /// <summary>
        /// Truncate a string to a given length
        /// </summary>
        /// <param name="value">String to truncate</param>
        /// <param name="maxLength">Length to which to truncate the string</param>
        /// <returns>The truncated string</returns>
        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
        #endregion Helpers
    }
}
