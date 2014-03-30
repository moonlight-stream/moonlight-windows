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

    /// <summary>
    /// UI Frame that contains the media element that streams Steam
    /// </summary>
    public partial class StreamFrame : PhoneApplicationPage
    {
        /// <summary>
        /// Width and height of the frame from the video source
        /// TODO Make these numbers less magic
        /// </summary>
        private int frameWidth = 1280;
        private int frameHeight = 720;

        /// <summary>
        /// Mouse input
        /// </summary>
        private int lastTouchX = 0;
        private int lastTouchY = 0;
        private bool hasMoved = false;
        MouseState ms;

        /// <summary>
        /// Gets and sets the custom video stream source
        /// </summary>
        internal VideoStreamSource VideoStream { get; private set; }

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
            VideoStream.EnqueueSamples(data.AsBuffer(), 0, 0);
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

        }

        public void ClStageComplete(int stage)
        {

        }

        public void ClStageFailed(int stage, int errorCode)
        {

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

            LimelightStreamConfiguration streamConfig = new LimelightStreamConfiguration(frameWidth, frameHeight, 60);
            LimelightDecoderRenderer drCallbacks = new LimelightDecoderRenderer(DrSetup, DrStart, DrStop, DrRelease, DrSubmitDecodeUnit);
            LimelightAudioRenderer arCallbacks = new LimelightAudioRenderer(ArInit, ArStart, ArStop, ArRelease, ArDecodeAndPlaySample);
            LimelightConnectionListener clCallbacks = new LimelightConnectionListener(ClStageStarting, ClStageComplete, ClStageFailed,
                ClConnectionStarted, ClConnectionTerminated, ClDisplayMessage, ClDisplayTransientMessage);

            //(uint)IPAddress.HostToNetworkOrder((int)IPAddress.Parse("192.168.1.207").Address)
            LimelightCommonRuntimeComponent.StartConnection(0xcf01a8c0, 
                streamConfig, clCallbacks, drCallbacks, arCallbacks);
        }

        #region Event Handlers

        /// <summary>
        /// Touch event initiated
        /// </summary>
        private void touchDownEvent(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            Debug.WriteLine("Hello. You have poked me");
            lastTouchX = ms.X;
            lastTouchY = ms.Y;
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
            ms = Mouse.GetState();

            Debug.WriteLine("meep");
            // If the user has moved
            if (ms.X != lastTouchX || ms.Y != lastTouchY)
            {
                hasMoved = true;
                LimelightCommonRuntimeComponent.SendMouseMoveEvent((short)(ms.X - lastTouchX),(short)(ms.Y - lastTouchY));

                lastTouchX = ms.X;
                lastTouchY = ms.Y; 
            }
        }

        #endregion Event Handlers
    }
}