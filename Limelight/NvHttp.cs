using Microsoft.Phone.Net.NetworkInformation;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Net.NetworkInformation;
using System.Linq; 

namespace Limelight
{
    public class NvHttp : IDisposable
    {
        private String uniqueId;
	    private String deviceName;
        private ManualResetEvent completeEvent;

	    public const int PORT = 47989;
	    public const int CONNECTION_TIMEOUT = 5000;
	    public String baseUrl;
        public IPAddress resolvedHost {get; set;}

        public NvHttp(String hostnameString)
        {
            completeEvent = new ManualResetEvent(false);
            ResolveHostName(hostnameString);
            this.baseUrl = "http://" + resolvedHost.ToString() + ":" + PORT;
            this.deviceName = GetDeviceName();
            this.uniqueId = GetDeviceName();
            Debug.WriteLine(deviceName);
            Debug.WriteLine(uniqueId);
        }

        /// <summary>
        /// Get the local device's name
        /// </summary>
        /// <returns>Mac address</returns>
        public String GetDeviceName()
        {
            return Microsoft.Phone.Info.DeviceStatus.DeviceName;
        }

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
                Debug.WriteLine("The IP address is " + ipAddress.ToString());
                this.resolvedHost = ipAddress;
            }
            completeEvent.Set();
        }

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
