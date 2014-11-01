namespace Limelight.Streaming
{
    using Limelight_common_binding;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.UI.Popups;
    using Windows.UI.Xaml.Controls;

    public class ConnectionManager
    {

        #region Stream Setup
        /// <summary>
        /// When the user presses "Start Streaming Steam", first check that they are paired
        /// </summary>
        public static async Task<StreamContext> StartStreaming(Computer computer, LimelightStreamConfiguration streamConfig)
        {
            PairingManager p = new PairingManager(computer); 

            // If we can't get the pair state, return   
            bool? pairState = await p.QueryPairState();
            if (!pairState.HasValue)
            {
                NotifyStreamSetupFailed("Pair state query failed");
                return null;
            }

            // If we're not paired, return
            if (pairState == false)
            {
                NotifyStreamSetupFailed("Device not paired");
                return null;
            }

            // Lookup the desired app in the app list
            // NOTE: This will go away when we have a proper app list
            int appId = await Task.Run(() => LookupAppIdForApp(new NvHttp(computer.IpAddress), "Steam"));
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
        /// Runs if checking pair state fails
        /// </summary>
        private static void NotifyStreamSetupFailed(string message)
        {
            Debug.WriteLine("Stream setup failed: "+message);
            var dialog = new MessageDialog(message, "Stream setup failed");
            dialog.ShowAsync();
        }

        /// <summary>
        /// Query the app list on the server to get the Steam App ID
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        private static int LookupAppIdForApp(NvHttp nv, String app)
        {
            XmlQuery appList;
            string appIdStr;

            try
            {
                appList = new XmlQuery(nv.BaseUrl + "/applist?uniqueid=" + nv.GetUniqueId());
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog(e.Message, "App List Query Failed");
                dialog.ShowAsync();
                return 0;
            }

            Debug.WriteLine(appList.rawXmlString);

            // App list query went well - try to get the app ID
            try
            {
                appIdStr = appList.SearchAttribute("App", "AppTitle", app, "ID");
                Debug.WriteLine(appIdStr);
                if (appIdStr == null)
                {
                    // Not found
                    var dialog = new MessageDialog("App Not Found", "App ID Lookup Failed");
                    dialog.ShowAsync();
                    return 0;
                }
            }
            // Exception connecting to the resource
            catch (Exception e)
            {
                // Steam ID lookup failed
                var dialog = new MessageDialog("Failed to get app ID: " + e.Message, "App ID Lookup Failed");
                dialog.ShowAsync();
                return 0;
            }

            // We're in the clear - save the app ID
            return Convert.ToInt32(appIdStr);
        }

        #endregion Helper Methods
    }
}