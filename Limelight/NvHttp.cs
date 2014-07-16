using Microsoft.Phone.Net.NetworkInformation;
using System;
using System.Net;
using System.Threading;

namespace Limelight
{
    /// <summary>
    /// Object containing the hostname and methods to resolve it
    /// </summary>
    public class NvHttp : IDisposable
    {
	    public const int PORT = 47989;
	    public const int CONNECTION_TIMEOUT = 5000;
        public String baseUrl { get; set; }
        public IPAddress resolvedHost {get; set;}

        private ManualResetEvent completeEvent;

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
            completeEvent = new ManualResetEvent(false);
            ResolveHostName(hostnameString);
            this.baseUrl = "http://" + resolvedHost.ToString() + ":" + PORT;
        }

        /// <summary>
        /// Get the local device's name
        /// </summary>
        /// <returns>Mac address</returns>
        public String GetDeviceName()
        {
            return Microsoft.Phone.Info.DeviceStatus.DeviceName;
        }
        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Resolve the GEForce PC hostname to an IP Address
        /// </summary>
        /// <param name="hostName"></param>
        private void ResolveHostName(String hostName)
        {
            var endPoint = new DnsEndPoint(hostName, 0);
            DeviceNetworkInformation.ResolveHostNameAsync(endPoint, OnNameResolved, null);
            // Wait for the callback to complete
            completeEvent.WaitOne();
            completeEvent.Dispose(); 
        }

        /// <summary>
        /// Callback for ResolveHostNameAsync
        /// </summary>
        /// <param name="result"></param>
        private void OnNameResolved(NameResolutionResult result)
        {
            IPEndPoint[] endpoints = result.IPEndPoints;

            // If resolved, it will provide me with an IP address
            if (endpoints != null && endpoints.Length > 0)
            {
                var ipAddress = endpoints[0].Address;
                this.resolvedHost = ipAddress;
            }
            else
            {
                throw new WebException("IP Address Malformed"); 
            }
        }
        #endregion Private Methods

        #region IDisposable implementation

        protected virtual void Dispose(bool managed)
        {
            if (managed)
            {
                completeEvent.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable implementation
    }
}
