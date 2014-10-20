namespace Limelight
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.UI.Popups;
    using Windows.UI.Xaml.Controls;

    public sealed partial class MainPage : Page
    {
        // TODO streamline what you're doing with nv
        NvHttp nv; 
        #region Stream Setup
        /// <summary>
        /// When the user presses "Start Streaming Steam", first check that they are paired
        /// </summary>
        private async Task StreamSetup(Computer computer)
        {
            // Resolve the hostname
            try
            {
                nv = new NvHttp(computer.IpAddress);
            }
            catch (Exception)
            {
                StreamSetupFailed("Invalid Hostname");
                return;
            }
           
            // Get the Ip address of the streaming machine
            try
            {
                await nv.ServerIPAddress();
            }
            catch (Exception)
            {
                StreamSetupFailed("Unable to get streaming machine's IP addresss");
                return; 
            }
            Pairing p = new Pairing(nv); 
            // HACK: Preload the cert data
            p.getClientCertificate();

            // If we can't get the pair state, return   
            bool? pairState = await p.QueryPairState();
            if (!pairState.HasValue)
            {
                StreamSetupFailed("Pair state query failed");
                return;
            }

            // If we're not paired, return
            if (pairState == false)
            {
                StreamSetupFailed("Device not paired");
                return;
            }
            // If we haven't cancelled and don't have the steam ID, query app list to get it
            if (computer.steamID == 0)
            {
                // If queryAppList fails, return
                computer.steamID = await Task.Run(() => QueryAppList());
                if (computer.steamID == 0)
                {
                    StreamSetupFailed("App list query failed");
                    return;
                }
            }
            StreamSetupComplete();
        }

        /// <summary>
        /// Runs upon successful completion of checking pair state when the user presses "Start Streaming Steam!"
        /// </summary>
        private void StreamSetupComplete()
        {
            selected.fps = getFps(); 

            // Pass the selected computer as the parameter
            this.Frame.Navigate(typeof(StreamFrame), selected);
        }

        /// <summary>
        /// Runs if checking pair state fails
        /// </summary>
        private void StreamSetupFailed(string message)
        {
            Debug.WriteLine("Stream setup failed");
            var dialog = new MessageDialog(message, "Stream setup failed");
            dialog.ShowAsync();
        }

        #endregion Stream Setup

        #region Helper Methods
        /// <summary>
        /// Query the app list on the server to get the Steam App ID
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        private int QueryAppList()
        {
            XmlQuery appList;
            string steamIdStr;
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
            // App list query went well - try to get the steam ID
            try
            {
                steamIdStr = appList.SearchAttribute("App", "AppTitle", "Steam", "ID");
                Debug.WriteLine(steamIdStr);
                if (steamIdStr == null)
                {
                    // Not found
                    var dialog = new MessageDialog("Steam Not Found", "Steam ID Lookup Failed");
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

        /// <summary>
        /// Get the selected FPS from the radio buttons
        /// </summary>
        /// <returns>FPS to use in the stream</returns>
        private int getFps()
        {
            if (_60fps_button.IsChecked == true)
            {
                return 60;
            }
            else
            {
                // 30 FPS button or null
                return 30; 
            }
        }

        /// <summary>
        /// Get the selected pixels from the radio buttons
        /// </summary>
        /// <returns>Pixels to use in the stream</returns>
        private int getPixels()
        {
            if (_1080p_button.IsChecked == true)
            {
                return 1080;
            }
            else
            {
                // 720p button or null
                return 720;
            }
        }

        #endregion Helper Methods
    }
}