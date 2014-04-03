using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Phone.Info;

namespace Limelight
{
    class Http
    {
        public static String getMacAddressString()
        {
            byte[] myDeviceID = (byte[])Microsoft.Phone.Info.DeviceExtendedProperties.GetValue("DeviceUniqueId");
            return BitConverter.ToString(myDeviceID);
        }

        public static String getDeviceName()
        {
            return (String)Microsoft.Phone.Info.DeviceExtendedProperties.GetValue("DeviceName");
        }
    }
}
