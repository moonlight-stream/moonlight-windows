using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Info;

namespace Limelight
{
    public class NvHttp
    {
        private String uniqueId;
	    private String deviceName;

	    public const int PORT = 47989;
	    public const int CONNECTION_TIMEOUT = 5000;

	    public String baseUrl;

        /* public NvHTTP(uint host, String uniqueId, String deviceName) {
		this.uniqueId = uniqueId;
		this.deviceName = deviceName;
		this.baseUrl = "http://" + host.getHostAddress() + ":" + PORT;
	}  */ // TODO 

        /// <summary>
        /// Get the local device's mac address
        /// </summary>
        /// <returns>Mac address</returns>
        public static String getMacAddressString()
        {
            byte[] myDeviceID = (byte[])Microsoft.Phone.Info.DeviceExtendedProperties.GetValue("DeviceUniqueId");
            return BitConverter.ToString(myDeviceID);
        }

        /// <summary>
        /// Gets the local device's name
        /// </summary>
        /// <returns>Device name</returns>
        public static String getDeviceName()
        {
            return (String)Microsoft.Phone.Info.DeviceExtendedProperties.GetValue("DeviceName");
        }

    }
}
