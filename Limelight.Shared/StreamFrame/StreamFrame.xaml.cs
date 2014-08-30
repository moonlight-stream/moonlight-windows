namespace Limelight
{
    using Limelight_common_binding;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.Graphics.Display;
    using Windows.Media.Core;
    using Windows.Media.MediaProperties;
    using Windows.Security.Cryptography;
    using Windows.Security.Cryptography.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Navigation;
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
        
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamFrame"/> class. 
        /// </summary>
        public StreamFrame()
        {
            InitializeComponent();

            // Audio/video stream source init
            AvStream = new AvStreamSource();

            // Show the progress bar
            Waitgrid.Visibility = Visibility.Visible;
            currentStateText.Visibility = Visibility.Visible;  
        }
        #endregion Constructor

        #region Navigation Events
        /// <summary>
        /// Get the computer information passed from the previous page
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // We only want to stream in landscape
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            selected = (Computer)e.Parameter;
        }
        
        /// <summary>
        /// Event handler for page loaded event
        /// </summary>
        private async void Loaded(object sender, RoutedEventArgs e)
        {
            StreamDisplay.Visibility = Visibility.Visible;
            Waitgrid.Visibility = Visibility.Collapsed;
            currentStateText.Visibility = Visibility.Collapsed; 
            
            // Hide the status bar
            var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
            await statusBar.HideAsync(); 
            
            LimelightStreamConfiguration config;

            byte[] aesKey = Pairing.GenerateRandomBytes(16);

            // GameStream only uses 4 bytes of a 16 byte IV. Go figure.
            byte[] aesRiIndex = Pairing.GenerateRandomBytes(4);
            byte[] aesIv = new byte[16];
            Array.ConstrainedCopy(aesRiIndex, 0, aesIv, 0, aesRiIndex.Length);
 
            config = new LimelightStreamConfiguration(frameWidth, frameHeight,
                30, 5000, 1024, aesKey, aesIv);
            InitializeMediaPlayer(config, AvStream);

            //H264FileReaderHackery h = new H264FileReaderHackery();
            //Task.Run(() => h.readFile(this));

            await StartConnection(config);
        } 
        #endregion Navigation Events

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
                LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Press, (int)MouseButton.Left);

                // Sleep here because some games do input detection by polling
                using (EventWaitHandle tmpEvent = new ManualResetEvent(false))
                {
                    tmpEvent.WaitOne(TimeSpan.FromMilliseconds(100));
                }

                // Raise the mouse button
                LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Release, (int)MouseButton.Left);
            }

        }
        /// <summary>
        /// Send mouse move event to the streaming PC
        /// </summary>
        private void MouseMove(object sender, PointerRoutedEventArgs e)
        {
            // FIXME
            var pointerCollection = e.GetIntermediatePoints(null);
            // TODO what the heck is this even giving me
            // TODO check for default value?? 
            var first = pointerCollection.FirstOrDefault();
            var last = pointerCollection.LastOrDefault();

            // TODO is this check redundant? Will the event trigger lie to me frequently enough (if ever) that it'll be an issue? 
            if (first.Position.X != last.Position.X || first.Position.Y != last.Position.Y)
            {
                short x = (short)(last.Position.X - first.Position.X); 
                short y = (short)(last.Position.Y - first.Position.Y);
                Debug.WriteLine(x + " and " + y);
                hasMoved = true;
                // Send the values to the streaming PC so it can register mouse movement
                LimelightCommonRuntimeComponent.SendMouseMoveEvent((short)(x), (short)(y));
            }            
            // TODO experimental code written without internet access = bad, nonfunctional code. FIXME
        }
        #endregion Mouse Events

        #region Keyboard events

        /// <summary>
        /// Send key down event to the streaming PC
        /// </summary>
        private void KeyDownHandler(object sender, KeyRoutedEventArgs e)
        {
            short key = KeyboardHelper.TranslateVirtualKey(e.Key);
            if (key != 0)
            {
                LimelightCommonRuntimeComponent.SendKeyboardEvent(key, (byte)KeyAction.Down,
                    KeyboardHelper.GetModifierFlags());
            }
        }

        /// <summary>
        /// Send key up event to the streaming PC
        /// </summary>
        private void KeyUpHandler(object sender, KeyRoutedEventArgs e)
        {
            short key = KeyboardHelper.TranslateVirtualKey(e.Key);
            if (key != 0)
            {
                LimelightCommonRuntimeComponent.SendKeyboardEvent(key, (byte)KeyAction.Up,
                    KeyboardHelper.GetModifierFlags());
            }
        }

        #endregion
    } 
}
