namespace Limelight
{
    using Limelight_common_binding;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.UI.Core;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    public sealed partial class StreamFrame : Page
    {
        #region Connection

        /// <summary>
        /// Starts the connection by calling into Limelight Common
        /// </summary>
        private async Task StartConnection()
        {
            NvHttp nv = null;
            await SetStateText("Resolving hostname...");
            try
            {
                nv = new NvHttp(selected.IpAddress);
            }
            catch (ArgumentNullException)
            {
                stageFailureText = "Error resolving hostname";
                ConnectionFailed();
            }

            XmlQuery launchApp;
            // Launch Steam
            await SetStateText("Launching Steam");
            try
            {
                launchApp = new XmlQuery(nv.baseUrl + "/launch?uniqueid=" + nv.GetDeviceName() + "&appid=" + selected.steamID);
            }
            catch (Exception)
            {
                Debug.WriteLine("Can't find steam");
                stageFailureText = "Error launching Steam";
                ConnectionFailed();
                return;
            }

            // Set up callbacks
            LimelightStreamConfiguration streamConfig = new LimelightStreamConfiguration(frameWidth, frameHeight, 30, 10000, 1024); // TODO a magic number. Get FPS from the settings
            LimelightDecoderRenderer drCallbacks = new LimelightDecoderRenderer(DrSetup, DrStart, DrStop, DrRelease, DrSubmitDecodeUnit);
            LimelightAudioRenderer arCallbacks = new LimelightAudioRenderer(ArInit, ArStart, ArStop, ArRelease, ArPlaySample);
            LimelightConnectionListener clCallbacks = new LimelightConnectionListener(ClStageStarting, ClStageComplete, ClStageFailed,
            ClConnectionStarted, ClConnectionTerminated, ClDisplayMessage, ClDisplayTransientMessage);

            // Call into Common to start the connection
            Debug.WriteLine("Starting connection");
            uint addr = 0;
            //uint addr = (uint)nv.resolvedHost.ToString(); // TODO how to get the addr as a uint
            LimelightCommonRuntimeComponent.StartConnection(addr, streamConfig, clCallbacks, drCallbacks, arCallbacks);

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