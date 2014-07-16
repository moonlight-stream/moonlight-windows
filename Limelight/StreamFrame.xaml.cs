using Limelight_common_binding;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation; 

namespace Limelight
{
    /// <summary>
    /// UI Frame that contains the streaming media element
    /// </summary>
    public partial class StreamFrame : PhoneApplicationPage
    {
        #region Class Variables

        /// <summary>
        /// App ID of Steam, received from the Main Page
        /// </summary>
        private int steamId;

        /// <summary>
        /// Connection stage identifiers
        /// </summary>
        private const int STAGE_NONE = 0;
        private const int STAGE_PLATFORM_INIT = 1;
        private const int STAGE_HANDSHAKE = 2;
        private const int STAGE_CONTROL_STREAM_INIT = 3;
        private const int STAGE_VIDEO_STREAM_INIT = 4;
        private const int STAGE_AUDIO_STREAM_INIT = 5;
        private const int STAGE_INPUT_STREAM_INIT = 6;
        private const int STAGE_CONTROL_STREAM_START = 7;
        private const int STAGE_VIDEO_STREAM_START = 8;
        private const int STAGE_AUDIO_STREAM_START = 9;
        private const int STAGE_INPUT_STREAM_START = 10;
        private const int STAGE_MAX = 11;

        /// <summary>
        /// Width and height of the frame from the video source
        /// TODO Make these numbers less magic
        /// </summary>
        private int frameWidth = 1280;
        private int frameHeight = 720;

        /// <summary>
        /// Mouse input
        /// </summary>
        private bool hasMoved = false;

        /// <summary>
        /// Gets and sets the custom AV source
        /// </summary>
        internal AvStreamSource AvStream { get; private set; }

        /// <summary>
        /// Background worker and callbacks
        /// </summary>
        private String stageFailureText;

        #endregion Class Variables

        #region Init
        /// <summary>
        /// Get the Steam App ID passed from the previous page
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (this.NavigationContext.QueryString.ContainsKey("steamId"))
            {
                steamId = Convert.ToInt32(this.NavigationContext.QueryString["steamId"]);
                Debug.WriteLine(steamId);
            }
            else
            {
                Debug.WriteLine("Error passing Steam ID");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamFrame"/> class. 
        /// </summary>
        public StreamFrame()
        {
            InitializeComponent();
            
            // TODO what uses this? 
            string parameter = string.Empty;

            // Audio/video stream source init
            AvStream = new AvStreamSource(frameWidth, frameHeight);
            StreamDisplay.SetSource(AvStream);
            StreamDisplay.AutoPlay = true;
            StreamDisplay.Play();

            // Show the progress bar
            Waitgrid.Visibility = Visibility.Visible;
            currentStateText.Visibility = Visibility.Visible;

        }

        #endregion Init

        #region Callbacks
        public void DrSetup(int width, int height, int redrawRate, int drFlags)
        {

        }

        public void DrStart()
        {

        }

        public void DrStop()
        {

        }

        public void DrRelease()
        {

        }

        public void DrSubmitDecodeUnit(byte[] data)
        {
            AvStream.EnqueueVideoSamples(data);
        }

        public void ArInit()
        {

        }

        public void ArStart()
        {

        }

        public void ArStop()
        {

        }

        public void ArRelease()
        {

        }

        public void ArPlaySample(byte[] data)
        {
            AvStream.EnqueueAudioSamples(data);
        }

        /// <summary>
        /// Stage beginning callback. Updates the connection progress bar with the current stage
        /// </summary>
        /// <param name="stage"></param>
        public void ClStageStarting(int stage)
        {
            String stateText = "";
            switch (stage)
            {
                case STAGE_PLATFORM_INIT:
                    stateText = "Initializing platform...";
                    break;
                case STAGE_HANDSHAKE:
                    stateText = "Starting handshake...";
                    break;
                case STAGE_CONTROL_STREAM_INIT:
                    stateText = "Initializing control stream...";
                    break;
                case STAGE_VIDEO_STREAM_INIT:
                    stateText = "Initializing video stream...";
                    break;
                case STAGE_AUDIO_STREAM_INIT:
                    stateText = "Initializing audio stream...";
                    break;
                case STAGE_INPUT_STREAM_INIT:
                    stateText = "Initializing input stream...";
                    break;
                case STAGE_CONTROL_STREAM_START:
                    stateText = "Starting control stream...";
                    break;
                case STAGE_VIDEO_STREAM_START:
                    stateText = "Starting video stream...";
                    break;
                case STAGE_AUDIO_STREAM_START:
                    stateText = "Starting audio stream...";
                    break;
                case STAGE_INPUT_STREAM_START:
                    stateText = "Starting input stream...";
                    break;
            }
            // Send the stage change to the UI thread. 
            // The dispatcher might not be quick enough for the user to see every stage
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() => SetStateText(stateText)));
        }

        /// <summary>
        /// Connection stage complete callback
        /// </summary>
        /// <param name="stage">Stage number</param>
        public void ClStageComplete(int stage)
        {

        }

        /// <summary>
        /// Connection stage failed callback
        /// </summary>
        /// <param name="stage">Stage number</param>
        /// <param name="errorCode">Error code for stage failure</param>
        public void ClStageFailed(int stage, int errorCode)
        {
            switch (stage)
            {
                case STAGE_PLATFORM_INIT:
                    stageFailureText = "Initializing platform failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_HANDSHAKE:
                    stageFailureText = "Starting handshake failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_CONTROL_STREAM_INIT:
                    stageFailureText = "Initializing control stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_VIDEO_STREAM_INIT:
                    stageFailureText = "Initializing video stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_AUDIO_STREAM_INIT:
                    stageFailureText = "Initializing audio stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_INPUT_STREAM_INIT:
                    stageFailureText = "Initializing input stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_CONTROL_STREAM_START:
                    stageFailureText = "Starting control stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_VIDEO_STREAM_START:
                    stageFailureText = "Starting video stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_AUDIO_STREAM_START:
                    stageFailureText = "Starting audio stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_INPUT_STREAM_START:
                    stageFailureText = "Starting input stream failed. Error: " + errorCode.ToString();
                    break;
            }
        }

        /// <summary>
        /// Connection stage started callback
        /// </summary>
        public void ClConnectionStarted()
        {

        }

        /// <summary>
        /// Connection stage terminated callback
        /// </summary>
        /// <param name="errorCode">Error code for connection terminated</param>
        public void ClConnectionTerminated(int errorCode)
        {
            Debug.WriteLine("Connection terminated: " + errorCode);
        }

        public void ClDisplayMessage(String message)
        {
        }

        public void ClDisplayTransientMessage(String message)
        {

        }

        #endregion Callbacks

        #region Connection

        /// <summary>
        /// Event handler for Background Worker's doWork event. Starts the connection.
        /// </summary>
        private async Task StartConnection()
        {
            NvHttp nv = null; 
            String hostnameString = (String)PhoneApplicationService.Current.State["host"];
            Dispatcher.BeginInvoke(new Action(() => SetStateText("Resolving hostname...")));
            // Resolve the hostname
            // TODO we're already resolving the hostname - why not just pass it to this page? 
            try
            {
                nv = new NvHttp(hostnameString);
            }
            catch (ArgumentNullException)
            {
                stageFailureText = "Error resolving hostname";
                ConnectionFailed(); 
            }
                
            XmlQuery launchApp; 
            // Launch Steam
            Dispatcher.BeginInvoke(new Action(() => SetStateText("Launching Steam...")));
            try
            {
                launchApp = new XmlQuery(nv.baseUrl + "/launch?uniqueid=" + nv.GetDeviceName() + "&appid=" + steamId);
            }
            catch (WebException)
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
            uint addr = (uint)nv.resolvedHost.Address;
            LimelightCommonRuntimeComponent.StartConnection(addr, streamConfig, clCallbacks, drCallbacks, arCallbacks);

            // If one of the stages failed, tell the background worker to cancel
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

        private void ConnectionFailed()
        {
            this.Waitgrid.Visibility = Visibility.Collapsed;
            this.currentStateText.Visibility = Visibility.Collapsed;
            // Inform the user of the failure via a message box
            MessageBoxResult result = MessageBox.Show(stageFailureText, "Failure Starting Connection", MessageBoxButton.OK);
            if (result == MessageBoxResult.OK)
            {
                // Return to the settings page
                Cleanup();
                // TODO this will need fixing with the new framework
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            }
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

        #region Touch Events

        /// <summary>
        /// Touch event initiated
        /// </summary>
        private void TouchDownEvent(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            MouseState ms = Mouse.GetState();
            hasMoved = false;
        }

        /// <summary>
        /// Event handler for mouse click - send mouse event to the streaming PC
        /// </summary>
        private void TouchUpEvent(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (!hasMoved)
            {
                // We haven't moved so send a click

                LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Press, (int)MouseButton.Left);

                // Sleep here because some games do input detection by polling
                try
                {
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Thread.sleep threw exception " + ex.StackTrace);
                }

                // Raise the mouse button
                LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Release, (int)MouseButton.Left);
            }
        }

        /// <summary>
        /// Event handler for mouse movement - send mouse move event to the streaming PC
        /// </summary>
        private void TouchMoveEvent(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            MouseState ms = Mouse.GetState();

            // If the user has moved
            if (ms.X != e.DeltaManipulation.Translation.X || ms.Y != e.DeltaManipulation.Translation.Y)
            {
                hasMoved = true;
                LimelightCommonRuntimeComponent.SendMouseMoveEvent((short)(ms.X - e.DeltaManipulation.Translation.X), (short)(ms.Y - e.DeltaManipulation.Translation.Y));
            }
        }
        #endregion Touch Events

        #region Helper methods
        /// <summary>
        /// Let the dispatcher set the state text on the progress bar
        /// </summary>
        /// <param name="stateText">The text to display on the progress bar</param>
        private void SetStateText(string stateText)
        {
            currentStateText.Text = stateText;
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        private void Cleanup()
        {
            this.steamId = 0;
            AvStream.Dispose();
            hasMoved = false; 
        }

        private async void Loaded(object sender, RoutedEventArgs e)
        {
            await StartConnection(); 
        }
    }
        #endregion Helper Methods
}