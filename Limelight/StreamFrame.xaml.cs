namespace Limelight
{
    using System.Net;
    using System.Threading;
    using System.Windows;
    using Limelight_common_binding;
    using Microsoft.Phone.Controls;
    using System.Diagnostics;
    using Microsoft.Xna.Framework.Input;
    using System;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.ComponentModel;

    /// <summary>
    /// UI Frame that contains the media element that streams Steam
    /// </summary>
    public partial class StreamFrame : PhoneApplicationPage
    {
        #region Class Variables
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
        /// Gets and sets the custom video stream source
        /// </summary>
        internal VideoStreamSource VideoStream { get; private set; }

        private BackgroundWorker bw = new BackgroundWorker();
        private String stageFailureText; 

        #endregion Class Variables

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
            Debug.WriteLine("Submitting decode unit of " + data.Length + " bytes");
            VideoStream.EnqueueSamples(data.AsBuffer(), 10, 10);
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

        public void ArDecodeAndPlaySample(byte[] data)
        {
            Debug.WriteLine("Playing audio of " + data.Length + " bytes");
        }

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
            Dispatcher.BeginInvoke(new Action(() => setStateText(stateText)));
        }

        public void ClStageComplete(int stage)
        {

        }

        public void ClStageFailed(int stage, int errorCode)
        {
            switch (stage)
            {
                case STAGE_PLATFORM_INIT:
                    stageFailureText = "Initializing platform failed.";
                    break;
                case STAGE_HANDSHAKE:
                    stageFailureText = "Starting handshake failed.";
                    break;
                case STAGE_CONTROL_STREAM_INIT:
                    stageFailureText = "Initializing control stream failed.";
                    break;
                case STAGE_VIDEO_STREAM_INIT:
                    stageFailureText = "Initializing video stream failed.";
                    break;
                case STAGE_AUDIO_STREAM_INIT:
                    stageFailureText = "Initializing audio stream failed.";
                    break;
                case STAGE_INPUT_STREAM_INIT:
                    stageFailureText = "Initializing input stream failed.";
                    break;
                case STAGE_CONTROL_STREAM_START:
                    stageFailureText = "Starting control stream failed.";
                    break;
                case STAGE_VIDEO_STREAM_START:
                    stageFailureText = "Starting video stream failed.";
                    break;
                case STAGE_AUDIO_STREAM_START:
                    stageFailureText = "Starting audio stream failed.";
                    break;
                case STAGE_INPUT_STREAM_START:
                    stageFailureText = "Starting input stream failed.";
                    break;
            }
        }

        public void ClConnectionStarted()
        {

        }

        public void ClConnectionTerminated(int errorCode)
        {
            Debug.WriteLine("Terminated: " + errorCode);
        }

        public void ClDisplayMessage(String message)
        {
        }

        public void ClDisplayTransientMessage(String message)
        {

        }

        #endregion Callbacks


        #region Background Worker

        /// <summary>
        /// Event handler for Background Worker's doWork event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bwDoWork(object sender, DoWorkEventArgs e)
        {
            LimelightStreamConfiguration streamConfig = new LimelightStreamConfiguration(frameWidth, frameHeight, 60);
            LimelightDecoderRenderer drCallbacks = new LimelightDecoderRenderer(DrSetup, DrStart, DrStop, DrRelease, DrSubmitDecodeUnit);
            LimelightAudioRenderer arCallbacks = new LimelightAudioRenderer(ArInit, ArStart, ArStop, ArRelease, ArDecodeAndPlaySample);
            LimelightConnectionListener clCallbacks = new LimelightConnectionListener(ClStageStarting, ClStageComplete, ClStageFailed,
            ClConnectionStarted, ClConnectionTerminated, ClDisplayMessage, ClDisplayTransientMessage);

            // Call into Common to start the connection
            // TODO give it a real host address
            LimelightCommonRuntimeComponent.StartConnection(0xcf01a8c0, streamConfig, clCallbacks, drCallbacks, arCallbacks);
            if(stageFailureText != null)
            {
                Debug.WriteLine("Operation cancelled");
                e.Cancel = true;
            }
        }

        // <summary>
        /// On completed do the appropriate task
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Waitgrid.Visibility = Visibility.Collapsed;
            this.currentStateText.Visibility = Visibility.Collapsed; 
            // Check to see if an error occurred in the background process.
            if (e.Error != null)
            {
                Debug.WriteLine("Error while performing background operation.");
            }
            else if(e.Cancelled) {
                // TODO Display a message box for the user
                // When the user presses cancel, it navigates them to the settings screen
                MessageBoxResult result = MessageBox.Show(stageFailureText, "Failure Starting Connection",  MessageBoxButton.OK);
                if (result == MessageBoxResult.OK)
                {
                    NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                }
            }
            else
            {
                // Everything completed normally.
                Debug.WriteLine("Background Worker Successfully Completed");

                StreamDisplay.Visibility = Visibility.Visible; 
            }

        }

        #endregion Background Worker

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamFrame"/> class. 
        /// </summary>
        public StreamFrame()
        {
            InitializeComponent();

            VideoStream = new VideoStreamSource(null, frameWidth, frameHeight);
            StreamDisplay.SetSource(VideoStream);  
            StreamDisplay.AutoPlay = true;
            StreamDisplay.Play();

            bw.WorkerReportsProgress = false;
            bw.WorkerSupportsCancellation = false; 

            bw.DoWork += new DoWorkEventHandler(bwDoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwRunWorkerCompleted);

            Waitgrid.Visibility = Visibility.Visible;
            currentStateText.Visibility = Visibility.Visible; 
            bw.RunWorkerAsync(); 

            //(uint)IPAddress.HostToNetworkOrder((int)IPAddress.Parse("192.168.1.207").Address)
        }

        #region Event Handlers

        /// <summary>
        /// Touch event initiated
        /// </summary>
        private void touchDownEvent(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            MouseState ms = Mouse.GetState();

            Debug.WriteLine("Hello. You have poked me");

            hasMoved = false; 
        }

        private void touchUpEvent(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            Debug.WriteLine("You have stopped touching me :( How sad.");
            if (!hasMoved)
            {
                // We haven't moved so send a click

                LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Press, (int)MouseButton.Left); 

                // Sleep here because some games do input detection by polling
                try
                {
                    Thread.Sleep(100);
                }
                catch (Exception ex) {
                    Debug.WriteLine("Thread.sleep threw exception " + ex.StackTrace);
                }

                // Raise the mouse button
                LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Release, (int)MouseButton.Left);
            }
        }

        private void touchMoveEvent(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            MouseState ms = Mouse.GetState();

            // If the user has moved
            if (ms.X != e.DeltaManipulation.Translation.X || ms.Y != e.DeltaManipulation.Translation.Y)
            {
                Debug.WriteLine("(" + (ms.X - e.DeltaManipulation.Translation.X) + ", " + (ms.Y - e.DeltaManipulation.Translation.Y) + ")");

                hasMoved = true;
                LimelightCommonRuntimeComponent.SendMouseMoveEvent((short)(ms.X - e.DeltaManipulation.Translation.X), (short)(ms.Y - e.DeltaManipulation.Translation.Y));
            }
        }

        /// <summary>
        /// Let the dispatcher set the state text on the progress bar
        /// </summary>
        /// <param name="stateText"></param>
        private void setStateText(string stateText)
        {
            currentStateText.Text = stateText; 
        }

        #endregion Event Handlers
    }
}