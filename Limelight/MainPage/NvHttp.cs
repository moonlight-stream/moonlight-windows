using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.System.Profile;

namespace Limelight
{    
    /// <summary>
    /// Object that obtains the base URL for http requests
    /// </summary>
    public class NvHttp
    {
        #region Class Variables

        public const int PORT = 47989; // FIXME: 47984
	    public const int CONNECTION_TIMEOUT = 5000;
        public string baseUrl { get; set; }
        public string serverIP {get; set; }

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
            if (string.IsNullOrWhiteSpace(hostnameString))
            {
                throw new ArgumentNullException("Invalid hostname");
            }
            this.hostname = hostnameString; 
        }
        #endregion Constructor

        #region Getters
        /// Get the local device's name
        /// </summary>
        /// <returns>Unique ID for the device</returns>
        public String GetDeviceName()
        {
            var token = HardwareIdentification.GetPackageSpecificToken(null);
            var hardwareId = token.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

            byte[] bytes = new byte[hardwareId.Length];
            dataReader.ReadBytes(bytes);

            return BitConverter.ToString(bytes); 
        }

        public async Task GetServerIPAddress()
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
                this.serverIP = this.hostname;
            }
            // Else, we need to resolve the hostname. 
            else
            {
                await ResolveHostName(this.hostname);
            }

            this.baseUrl = "http://" + serverIP + ":" + PORT; // FIXME: https
        }
        #endregion Getters

        #region Hostname resolution

        /// <summary>
        /// Resolve the GEForce PC hostname to an IP Address
        /// </summary>
        /// <param name="hostName"></param>
        private async Task ResolveHostName(String hostName)
        {
            HostName serverHost = new HostName(hostName);
            StreamSocket clientSocket = new Windows.Networking.Sockets.StreamSocket();

            // Try to connect to the remote host
            try
            {
                await clientSocket.ConnectAsync(serverHost, "http");

            }
                // TODO properly handle this exception
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.Message);
            }

            this.serverIP = clientSocket.Information.RemoteAddress.ToString();
           
        }
        #endregion Hostname resolution
    }
}
