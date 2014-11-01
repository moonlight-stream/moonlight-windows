namespace Limelight
{
    using Limelight_common_binding;
    using System;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Windows.UI.Core;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    public sealed partial class StreamFrame : Page
    {
        #region Connection

        /// <summary>
        /// Create start HTTP request
        /// </summary>
        private async Task<XmlQuery> StartOrResumeApp(NvHttp nv, LimelightStreamConfiguration streamConfig)
        {
            XmlQuery serverInfo = new XmlQuery(nv.BaseUrl + "/serverinfo?uniqueid=" + nv.GetUniqueId());
            string currentGameString = await serverInfo.ReadXmlAttribute("currentgame");
            if (currentGameString == null)
            {
                return null;
            }

            byte[] aesIv = streamConfig.GetRiAesIv();
            int riKeyId =
                (int)(((aesIv[0] << 24) & 0xFF000000U) |
                ((aesIv[1] << 16) & 0xFF0000U) |
                ((aesIv[2] << 8) & 0xFF00U) |
                (aesIv[3] & 0xFFU));
            string riConfigString =
                "&rikey=" + PairingCryptoHelpers.BytesToHex(streamConfig.GetRiAesKey()) +
                "&rikeyid=" + riKeyId;

            // Launch a new game if nothing is running
            if (currentGameString == null || currentGameString.Equals("0"))
            {
                return new XmlQuery(nv.BaseUrl + "/launch?uniqueid=" + nv.GetUniqueId() + "&appid=" + context.appId +
                    "&mode=" + streamConfig.GetWidth() + "x" + streamConfig.GetHeight() + "x" + streamConfig.GetFps() +
                    "&additionalStates=1&sops=1" + // FIXME: make sops configurable
                    riConfigString);
            }
            else
            {
                // A game was already running, so resume it
                // FIXME: Quit and relaunch if it's not the game we came to start
                return new XmlQuery(nv.BaseUrl + "/resume?uniqueid=" + nv.GetUniqueId() + riConfigString);
            }
        }

        /// <summary>
        /// Starts the connection by calling into Limelight Common
        /// </summary>
        private async Task StartConnection(LimelightStreamConfiguration streamConfig)
        {
            NvHttp nv = null;
            await SetStateText("Resolving hostname...");
            try
            {
                nv = new NvHttp(context.computer.IpAddress);
            }
            catch (ArgumentNullException)
            {
                stageFailureText = "Error resolving hostname";
                ConnectionFailed();
                return;
            }

            String serverIp = null;
            try
            {
                serverIp = await nv.ResolveServerIPAddress();
            }
            catch (Exception)
            {
                stageFailureText = "Error resolving hostname";
                ConnectionFailed();
                return;
            }

            // Set up callbacks
            LimelightDecoderRenderer drCallbacks = new LimelightDecoderRenderer(DrSetup, DrStart, DrStop, DrRelease, DrSubmitDecodeUnit);
            LimelightAudioRenderer arCallbacks = new LimelightAudioRenderer(ArInit, ArStart, ArStop, ArRelease, ArPlaySample);
            LimelightConnectionListener clCallbacks = new LimelightConnectionListener(ClStageStarting, ClStageComplete, ClStageFailed,
            ClConnectionStarted, ClConnectionTerminated, ClDisplayMessage, ClDisplayTransientMessage);
            LimelightPlatformCallbacks plCallbacks = new LimelightPlatformCallbacks(PlThreadStart, PlDebugPrint);

            XmlQuery launchApp;
            // Launch Steam
            await SetStateText("Launching Steam");
            try
            {
                launchApp = await StartOrResumeApp(nv, streamConfig);
            }
            catch (Exception)
            {
                launchApp = null;
            }

            if (launchApp == null)
            {
                Debug.WriteLine("Can't find app");
                stageFailureText = "Error launching App";
                ConnectionFailed();
                return;
            }

            // Call into Common to start the connection
            Debug.WriteLine("Starting connection");

            Regex r = new Regex(@"^(?<octet1>\d+).(?<octet2>\d+).(?<octet3>\d+).(?<octet4>\d+)");
            Match m = r.Match(serverIp);

            uint addr = (uint)(Convert.ToByte(m.Groups["octet4"].Value) << 24 |
                Convert.ToByte(m.Groups["octet3"].Value) << 16 | 
                Convert.ToByte(m.Groups["octet2"].Value) << 8 |
                Convert.ToByte(m.Groups["octet1"].Value));
            LimelightCommonRuntimeComponent.StartConnection(addr, streamConfig, clCallbacks, drCallbacks, arCallbacks, plCallbacks);

            if (stageFailureText != null)
            {
                Debug.WriteLine("Stage failed");
                ConnectionFailed();
                return;
            }
            else
            {
                ConnectionSuccess();
            }
        }

        /// <summary>
        /// Runs if starting the connection failed
        /// </summary>
        private async void ConnectionFailed()
        {
            // Stop showing the wait UI
            this.Waitgrid.Visibility = Visibility.Collapsed;
            this.currentStateText.Visibility = Visibility.Collapsed;

            // Inform the user of the failure via a message dialog            
            var dialog = new MessageDialog(stageFailureText, "Starting Connection Failed");
            dialog.Commands.Add(new UICommand("ok", x =>
            {
                Cleanup();
                this.Frame.Navigate(typeof(MainPage));
            }));

            await dialog.ShowAsync();
        }

        /// <summary>
        /// Runs if starting the connection was successful. 
        /// </summary>
        private void ConnectionSuccess()
        {
            Debug.WriteLine("Connection Successfully Completed");
            this.Waitgrid.Visibility = Visibility.Collapsed;
            this.currentStateText.Visibility = Visibility.Collapsed;
            StreamDisplay.Visibility = Visibility.Visible;
        }
        #endregion Connection

        #region Helper methods
        /// <summary>
        /// Set the state text on the progress bar
        /// </summary>
        /// <param name="stateText">The text to display on the progress bar</param>
        private async Task SetStateText(string stateText)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => currentStateText.Text = stateText);
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        private void Cleanup()
        {
            // TODO will this be okay if we haven't started a connection? 
            LimelightCommonRuntimeComponent.StopConnection();
            hasMoved = false;
        }
        #endregion Helper methods
    }
}