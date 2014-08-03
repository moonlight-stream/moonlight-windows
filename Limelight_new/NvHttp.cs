using Windows.Networking;
using Windows.Networking.Sockets; 
using System;
using System.Net;
using System.Threading;
using Windows.System.Profile;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Limelight_new
{
    
    /// <summary>
    /// Object containing the hostname and methods to resolve it
    /// </summary>
    public class NvHttp 
    {
        private Regex IP = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
        public const int PORT = 47989;
	    public const int CONNECTION_TIMEOUT = 5000;
        public String baseUrl { get; set; }
        public string resolvedHost {get; set;}

        #region Public Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="NvHttp"/> class. 
        /// </summary>
        /// <param name="hostnameString">Hostname or IP address of the streaming machine</param>
        public NvHttp(String hostnameString)
        {
            if (string.IsNullOrWhiteSpace(hostnameString))
            {
                throw new ArgumentNullException("Invalid hostname");
            }
            Debug.WriteLine("Resolving host");
            Match ipAddr = null; 

            // Check if it's an IP address as-is based on a regex match
            try {
              ipAddr = IP.Match(hostnameString);
            } catch (Exception ex){
                Debug.WriteLine("Exception: " + ex.Message);
            }
            if(ipAddr.Success) {
                this.resolvedHost = hostnameString; 
            }
            else
            {
                Task.Run(async () => await ResolveHostName(hostnameString)).Wait();
            }


            this.baseUrl = "http://" + resolvedHost + ":" + PORT;
        }

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
        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Resolve the GEForce PC hostname to an IP Address
        /// </summary>
        /// <param name="hostName"></param>
        private async Task ResolveHostName(String hostName)
        {
            HostName serverHost = new HostName(hostName);
            StreamSocket clientSocket = new Windows.Networking.Sockets.StreamSocket();

            // Try to connect to the remote host
            // TODO do we need try/catch here? 
            try
            {
                await clientSocket.ConnectAsync(serverHost, "http");

            }
            catch (Exception e)
            {
                Debug.WriteLine("Problem!!! " + e.Message);
            }

            this.resolvedHost = clientSocket.Information.RemoteAddress.ToString();
           
        }
        #endregion Private Methods
    }
}
