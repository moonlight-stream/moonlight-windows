using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
//using Limelight_common_binding;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Limelight_new
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StreamFrame : Page
    {

        #region Class Variables

        /// <summary>
        /// Selected computer passed from the main page
        /// </summary>
        Computer selected;

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
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            selected = (Computer)e.Parameter;           
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamFrame"/> class. 
        /// </summary>
        public StreamFrame()
        {
            InitializeComponent();

            // Audio/video stream source init
            AvStream = new AvStreamSource(frameWidth, frameHeight);
            //StreamDisplay.SetSource(AvStream, "TODO WHAT IS THE MIME TYPE");
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
            //AvStream.EnqueueVideoSamples(data);
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
            //AvStream.EnqueueAudioSamples(data);
        }

        /// <summary>
        /// Stage beginning callback. Updates the connection progress bar with the current stage
        /// </summary>
        /// <param name="stage"></param>
        public async void ClStageStarting(int stage)
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
            await SetStateText(stateText);
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
        /// Connection terminated callback
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
            //LimelightStreamConfiguration streamConfig = new LimelightStreamConfiguration(frameWidth, frameHeight, 30, 10000, 1024); // TODO a magic number. Get FPS from the settings
            //LimelightDecoderRenderer drCallbacks = new LimelightDecoderRenderer(DrSetup, DrStart, DrStop, DrRelease, DrSubmitDecodeUnit);
            //LimelightAudioRenderer arCallbacks = new LimelightAudioRenderer(ArInit, ArStart, ArStop, ArRelease, ArPlaySample);
            //LimelightConnectionListener clCallbacks = new LimelightConnectionListener(ClStageStarting, ClStageComplete, ClStageFailed,
            //ClConnectionStarted, ClConnectionTerminated, ClDisplayMessage, ClDisplayTransientMessage);

            // Call into Common to start the connection
            Debug.WriteLine("Starting connection");
            // uint addr = (uint)nv.resolvedHost; // TODO how to get the addr as a uint
            //LimelightCommonRuntimeComponent.StartConnection(addr, streamConfig, clCallbacks, drCallbacks, arCallbacks);

            // If one of the stages failed, stop
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

        #region Mouse Events

        /// <summary>
        /// Send mouse down event to the streaming PC
        /// </summary>
        private void MouseDown(object sender, PointerRoutedEventArgs e)
        {
            hasMoved = false; 
        }
        /// <summary>
        /// Send mouse click event to the streaming PC
        /// </summary>
        private void MouseUp(object sender, PointerRoutedEventArgs e)
        {
            if (!hasMoved)
            {
                // We haven't moved so send a click
                // TODO what even is this pointer crap. How do I send a click? 
                //LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Press, (int)MouseButton.Left);

                // Sleep here because some games do input detection by polling
                try
                {
                    // TODO how do we sleep
                    // Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Thread.sleep threw exception " + ex.StackTrace);
                }

                // Raise the mouse button
                //LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Release, (int)MouseButton.Left);
            }

        }
        /// <summary>
        /// Send mouse move event to the streaming PC
        /// </summary>
        private void MouseMove(object sender, PointerRoutedEventArgs e)
        {
            var pointerCollection = e.GetIntermediatePoints(null);
            // TODO what the heck is this even giving me
            // TODO check for default value?? 
            var first = pointerCollection.FirstOrDefault();
            var last = pointerCollection.LastOrDefault();

            // TODO is this check redundant? 
            if (first.Position.X != last.Position.X || first.Position.Y != last.Position.Y)
            {
                short x = (short)(last.Position.X - first.Position.X); 
                short y = (short)(last.Position.Y - first.Position.Y);
                Debug.WriteLine(x + " and " + y);
                hasMoved = true;
                // Send the values to the streaming PC so it can register mouse movement
                //LimelightCommonRuntimeComponent.SendMouseMoveEvent((short)(x), (short)(y));
            }            

        }
        #endregion Mouse Events

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
            AvStream.Dispose();
            hasMoved = false; 
        }

        /// <summary>
        /// Event handler for page loaded event
        /// </summary>
        private async void Loaded(object sender, RoutedEventArgs e)
        {
            // TODO figure out why VS is yelling at me
            await StartConnection(); 
        }
        #endregion Helper Methods 




    } 
}
