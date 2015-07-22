namespace Moonlight
{
    using Moonlight.Utils;
    using Moonlight_common_binding;
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
        private int serverMajorVersion;

        #region Connection

        /// <summary>
        /// Create start HTTP request
        /// </summary>
        private async Task<bool> StartOrResumeApp(NvHttp nv, MoonlightStreamConfiguration streamConfig)
        {
            XmlQuery serverInfo = new XmlQuery(nv.BaseUrl + "/serverinfo?uniqueid=" + nv.GetUniqueId());
            string currentGameString = await serverInfo.ReadXmlElement("currentgame");
            if (currentGameString == null)
            {
                return false;
            }

            string versionString = await serverInfo.ReadXmlElement("appversion");
            if (versionString == null)
            {
                return false;
            }

            serverMajorVersion = Convert.ToInt32(versionString.Substring(0, 1));

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
                XmlQuery x = new XmlQuery(nv.BaseUrl + "/launch?uniqueid=" + nv.GetUniqueId() + "&appid=" + context.appId +
                    "&mode=" + streamConfig.GetWidth() + "x" + streamConfig.GetHeight() + "x" + streamConfig.GetFps() +
                    "&additionalStates=1&sops=1" + // FIXME: make sops configurable
                    riConfigString);

                string sessionStr = await x.ReadXmlElement("gamesession");
                if (sessionStr == null || sessionStr.Equals("0"))
                {
                    return false;
                }

                return true;
            }
            else
            {
                // A game was already running, so resume it
                // FIXME: Quit and relaunch if it's not the game we came to start
                XmlQuery x = new XmlQuery(nv.BaseUrl + "/resume?uniqueid=" + nv.GetUniqueId() + riConfigString);

                string resumeStr = await x.ReadXmlElement("resume");
                if (resumeStr == null || resumeStr.Equals("0"))
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Starts the connection by calling into Moonlight Common
        /// </summary>
        private async Task StartConnection(MoonlightStreamConfiguration streamConfig)
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
            MoonlightDecoderRenderer drCallbacks = new MoonlightDecoderRenderer(DrSetup, DrCleanup, DrSubmitDecodeUnit);
            MoonlightAudioRenderer arCallbacks = new MoonlightAudioRenderer(ArInit, ArCleanup, ArPlaySample);
            MoonlightConnectionListener clCallbacks = new MoonlightConnectionListener(ClStageStarting, ClStageComplete, ClStageFailed,
            ClConnectionStarted, ClConnectionTerminated, ClDisplayMessage, ClDisplayTransientMessage);

            // Launch Steam
            await SetStateText("Launching Steam");
            if (await StartOrResumeApp(nv, streamConfig) == false)
            {
                Debug.WriteLine("Can't find app");
                stageFailureText = "Error launching App";
                ConnectionFailed();
                return;
            }

            // Call into Common to start the connection
            Debug.WriteLine("Starting connection");

            MoonlightCommonRuntimeComponent.StartConnection(serverIp, streamConfig, clCallbacks, drCallbacks, arCallbacks, serverMajorVersion);

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
        private void ConnectionFailed()
        {
            // Stop showing the wait UI
            this.Waitgrid.Visibility = Visibility.Collapsed;
            this.currentStateText.Visibility = Visibility.Collapsed;

            // Inform the user of the failure via a message dialog  
            DialogUtils.DisplayDialog(this.Dispatcher, stageFailureText, "Starting Connection Failed", x =>
            {
                Cleanup();
                this.Frame.Navigate(typeof(MainPage));
            });
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
            MoonlightCommonRuntimeComponent.StopConnection();
            hasMoved = false;
        }
        #endregion Helper methods
    }
}