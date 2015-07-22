namespace Moonlight.Streaming
{
    using Moonlight.Utils;
    using Moonlight_common_binding;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.UI.Core;
    using Windows.UI.Popups;
    using Windows.UI.Xaml.Controls;

    public class ConnectionManager
    {

        #region Stream Setup
        /// <summary>
        /// When the user presses "Start Streaming Steam", first check that they are paired
        /// </summary>
        public static async Task<StreamContext> StartStreaming(CoreDispatcher uiDispatcher, Computer computer, MoonlightStreamConfiguration streamConfig)
        {
            PairingManager p = new PairingManager(computer); 

            // If we can't get the pair state, return   
            bool? pairState = await p.QueryPairState();
            if (!pairState.HasValue)
            {
                DialogUtils.DisplayDialog(uiDispatcher, "Pair state query failed", "Failed to start streaming");
                return null;
            }

            // If we're not paired, return
            if (pairState == false)
            {
                DialogUtils.DisplayDialog(uiDispatcher, "Device not paired", "Failed to start streaming");
                return null;
            }

            // Lookup the desired app in the app list
            // NOTE: This will go away when we have a proper app list
            int appId = await LookupAppIdForApp(uiDispatcher, new NvHttp(computer.IpAddress), "Steam");
            if (appId == 0)
            {
                // LookupAppIdForApp() handles displaying a failure dialog
                return null;
            }

            return new StreamContext(computer, appId, streamConfig);
        }

        #endregion Stream Setup

        #region Helper Methods

        /// <summary>
        /// Query the app list on the server to get the Steam App ID
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        private static async Task<int> LookupAppIdForApp(CoreDispatcher dispatcher, NvHttp nv, String app)
        {
            XmlQuery appList;
            string appIdStr;

            appList = new XmlQuery(nv.BaseUrl + "/applist?uniqueid=" + nv.GetUniqueId());

            // App list query went well - try to get the app ID
            try
            {
                appIdStr = await appList.SearchElement("App", "AppTitle", app, "ID");
                Debug.WriteLine(appIdStr);
                if (appIdStr == null)
                {
                    // Not found
                    DialogUtils.DisplayDialog(dispatcher, "App Not Found", "App ID Lookup Failed");
                    return 0;
                }
            }
            // Exception connecting to the resource
            catch (Exception e)
            {
                // Steam ID lookup failed
                DialogUtils.DisplayDialog(dispatcher, "Failed to get app ID: " + e.Message, "App ID Lookup Failed");
                return 0;
            }

            // We're in the clear - save the app ID
            return Convert.ToInt32(appIdStr);
        }

        #endregion Helper Methods
    }
}