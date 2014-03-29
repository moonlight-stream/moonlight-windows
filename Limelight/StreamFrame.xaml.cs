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

            // TODO play the stream. ha.
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

                //conn.sendMouseButtonDown((byte) 0x01); 

                // Sleep here because some games do input detection by polling
                try
                {
                    Thread.Sleep(100);
                }
                catch (Exception ex) {
                    Debug.WriteLine("Thread.sleep threw exception " + ex.StackTrace);
                }

                // Raise the mouse button
                // conn.sendMouseButtonUp((byte) 0x01); 
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
                //conn.sendMouseMove((short)(ms.X - lastTouchX),(short)(ms.Y - lastTouchY));

                lastTouchX = ms.X;
                lastTouchY = ms.Y; 
            }
        }

        #endregion Event Handlers
    }
}