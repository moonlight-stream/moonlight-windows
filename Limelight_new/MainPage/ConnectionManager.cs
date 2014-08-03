namespace Limelight_new
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.UI.Popups;
    using Windows.UI.Xaml.Controls;

    public sealed partial class MainPage : Page
    {
        #region Stream Setup
        /// <summary>
        /// When the user presses "Start Streaming Steam", first check that they are paired in the background worker
        /// </summary>
        private async Task StreamSetup(string uri)
        {
            try
            {
                nv = new NvHttp(uri);
            }
            catch (Exception)
            {
                StreamSetupFailed("Invalid Hostname");
                return;
            }
           
            try
            {
                await nv.GetServerIPAddress();
            }
            catch (Exception)
            {
                StreamSetupFailed("Unable to get streaming machine's IP addresss"); 
            }

            // If device is already paired, return.             
            if (!await QueryPairState())
            {
                await StreamSetupFailed("Pair state query failed");
                return;
            }

            // If we haven't cancelled and don't have the steam ID, query app list to get it
            if (steamId == 0)
            {
                // If queryAppList fails, return
                if (!await Task.Run(() => QueryAppList()))
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
        private async Task<bool> QueryAppList()
        {
            XmlQuery appList;
            string steamIdStr;
            try
            {
                appList = new XmlQuery(nv.baseUrl + "/applist?uniqueid=" + nv.GetDeviceName());
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Device not paired: " + e.Message, "App List Query Failed");
                dialog.ShowAsync();
                return false;
            }
            // App list query went well - try to get the steam ID
            try
            {
                steamIdStr = await Task.Run(() => appList.XmlAttribute("ID", appList.XmlAttributeElement("App")));

            }
            catch (Exception e)
            {
                // Steam ID lookup failed
                var dialog = new MessageDialog("Failed to get Steam ID: " + e.Message, "Steam ID Lookup Failed");
                dialog.ShowAsync();
                return false;
            }

            // We're in the clear - save the Steam app ID
            steamId = Convert.ToInt32(steamIdStr);
            return true;
        }

        #endregion Helper Methods
    }
}