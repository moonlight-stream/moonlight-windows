using Windows.Networking;
using Windows.Networking.Sockets; 
using System;
using System.Net;
using System.Threading;
using Windows.System.Profile;
using Windows.Storage.Streams;
using System.Threading.Tasks;

namespace Limelight_new
{
    /// <summary>
    /// Object containing the hostname and methods to resolve it
    /// </summary>
    public class NvHttp 
    {
	    public const int PORT = 47989;
	    public const int CONNECTION_TIMEOUT = 5000;
        public String baseUrl { get; set; }
        public HostName resolvedHost {get; set;}

        #region Public Methods
        /// <summary>
        /// Initializes a new instance of the <see cref="NvHttp"/> class. 
        /// </summary>
        /// <param name="hostnameString">Hostname or IP address of the streaming machine</param>
        public NvHttp(String hostnameString)
        {
            if (hostnameString == null)
            {
                throw new ArgumentNullException("Hostname cannot be null");
            }
            // TODO where's hostname resolution exception handling happening
            Task.Run(async () => await ResolveHostName(hostnameString)).Wait();
            this.baseUrl = "http://" + resolvedHost.ToString() + ":" + PORT;
        }

        /// <summary>
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
            await clientSocket.ConnectAsync(serverHost, "http");

            this.resolvedHost = clientSocket.Information.RemoteAddress;
           
        }
        #endregion Private Methods
    }
}
