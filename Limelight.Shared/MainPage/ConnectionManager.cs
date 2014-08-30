namespace Limelight
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.UI.Popups;
    using Windows.UI.Xaml.Controls;

    public sealed partial class MainPage : Page
    {
        NvHttp nv; 
        #region Stream Setup
        /// <summary>
        /// When the user presses "Start Streaming Steam", first check that they are paired in the background worker
        /// </summary>
        private async Task StreamSetup(Computer computer)
        {
            try
            {
                nv = new NvHttp(computer.IpAddress);
            }
            catch (Exception)
            {
                StreamSetupFailed("Invalid Hostname");
                return;
            }
           
            try
            {
                await nv.ServerIPAddress();
            }
            catch (Exception)
            {
                StreamSetupFailed("Unable to get streaming machine's IP addresss"); 
            }
            Pairing p = new Pairing(nv); 
            // HACK: Preload the cert data
            p.getClientCertificate();

            // If device is already paired, return.             
            if (!await p.QueryPairState())
            {
                await StreamSetupFailed("Pair state query failed");
                return;
            }

            // If we haven't cancelled and don't have the steam ID, query app list to get it
            if (computer.steamID == 0)
            {
                // If queryAppList fails, return
                computer.steamID = await Task.Run(() => QueryAppList());
                if (computer.steamID == 0)
                {
                    await StreamSetupFailed("App list query failed");
                    return;
                }
            }
            await StreamSetupComplete();
        }

        /// <summary>
        /// Runs upon successful completion of checking pair state when the user presses "Start Streaming Steam!"
        /// </summary>
        private async Task StreamSetupComplete()
        {
            // Pass the selected computer as the parameter
            this.Frame.Navigate(typeof(StreamFrame), selected);
        }

        /// <summary>
        /// Runs if checking pair state fails
        /// </summary>
        private async Task StreamSetupFailed(string message)
        {
            Debug.WriteLine("Stream setup failed");
            var dialog = new MessageDialog(message, "Stream setup failed");
            await dialog.ShowAsync();
        }

        #endregion Stream Setup

        #region Helper Methods
        /// <summary>
        /// Query the app list on the server to get the Steam App ID
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        private async Task<int> QueryAppList()
        {
            XmlQuery appList;
            string steamIdStr;
            try
            {
                appList = new XmlQuery(nv.baseUrl + "/applist?uniqueid=" + nv.GetUniqueId());
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Device not paired: " + e.Message, "App List Query Failed");
                dialog.ShowAsync();
                return 0;
            }
            Debug.WriteLine(appList.rawXmlString); 
            // App list query went well - try to get the steam ID
            try
            {
                // FIXME Due to a bug in Steam, we need to launch by app right now to test
                steamIdStr = appList.SearchAttribute("App", "AppTitle", "Borderlands 2", "ID");
                Debug.WriteLine(steamIdStr);
                if (steamIdStr == null)
                {
                    // Not found
                    var dialog = new MessageDialog("Steam ID Not Found", "Steam ID Lookup Failed");
                    dialog.ShowAsync();
                    return 0;
                }
            }
                // Exception connecting to the resource
            catch (Exception e)
            {
                // Steam ID lookup failed
                var dialog = new MessageDialog("Failed to get Steam ID: " + e.Message, "Steam ID Lookup Failed");
                dialog.ShowAsync();
                return 0;
            }

            // We're in the clear - save the Steam app ID
            return Convert.ToInt32(steamIdStr);
        }

        #endregion Helper Methods
    }
}