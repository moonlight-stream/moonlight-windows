namespace Limelight
{
    using Limelight_common_binding;
    using Limelight.Streaming;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Windows.Devices.Input;
    using Windows.Graphics.Display;
    using Windows.Media.Core;
    using Windows.Media.MediaProperties;
    using Windows.Security.Cryptography;
    using Windows.Security.Cryptography.Core;
    using Windows.UI.Core;
    using Windows.UI.Input;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Navigation;
    public sealed partial class StreamFrame : Page
    {
        #region Class Variables

        /// <summary>
        /// Context passed from the main page
        /// </summary>
        private StreamContext context;

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
        /// Mouse input
        /// </summary>
        private bool hasMoved = false;
        private int mouseButtonFlag;
        private short lastX = 0;
        private short lastY = 0;
        private const int MOUSE_BUTTON_LEFT = 0x1;
        private const int MOUSE_BUTTON_MIDDLE = 0x2;
        private const int MOUSE_BUTTON_RIGHT = 0x4;

        /// <summary>
        /// Gets and sets the custom AV source
        /// </summary>
        internal AvStreamSource AvStream { get; private set; }

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
            context = (StreamContext)e.Parameter;

            Window.Current.CoreWindow.KeyDown += WindowKeyDownHandler;
            Window.Current.CoreWindow.KeyUp += WindowKeyUpHandler;
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
            //var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
            //await statusBar.HideAsync(); 

            InitializeMediaPlayer(context.streamConfig, AvStream);

            await StartConnection(context.streamConfig);
        }
        #endregion Navigation Events

        #region Mouse Events

        private static int GetButtonFlags(PointerPoint ptrPt)
        {
            int mouseButtonFlag = 0;

            if (ptrPt.Properties.IsLeftButtonPressed)
            {
                mouseButtonFlag |= MOUSE_BUTTON_LEFT;
            }
            if (ptrPt.Properties.IsMiddleButtonPressed)
            {
                mouseButtonFlag |= MOUSE_BUTTON_MIDDLE;
            }
            if (ptrPt.Properties.IsRightButtonPressed)
            {
                mouseButtonFlag |= MOUSE_BUTTON_RIGHT;
            }

            return mouseButtonFlag;
        }

        /// <summary>
        /// Send mouse down event to the streaming PC
        /// </summary>
        private void MouseDown(object sender, PointerRoutedEventArgs e)
        {
            Pointer ptr = e.Pointer;
            PointerPoint ptrPt = e.GetCurrentPoint(StreamDisplay);

            // If using a mouse, then get the correct button
            if (ptr.PointerDeviceType == PointerDeviceType.Mouse)
            {

                // Send changes and update the current state
                int deltaButtons = mouseButtonFlag ^ GetButtonFlags(ptrPt);
                if ((deltaButtons & MOUSE_BUTTON_LEFT) != 0)
                {
                    LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Press,
                        (int)MouseButton.Left);
                }
                if ((deltaButtons & MOUSE_BUTTON_MIDDLE) != 0)
                {
                    LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Press,
                        (int)MouseButton.Middle);
                }
                if ((deltaButtons & MOUSE_BUTTON_RIGHT) != 0)
                {
                    LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Press,
                        (int)MouseButton.Right);
                }
                mouseButtonFlag = GetButtonFlags(ptrPt);
            }
            else
            {
                // We haven't moved yet
                hasMoved = false;
                lastX = (short)ptrPt.Position.X;
                lastY = (short)ptrPt.Position.Y; 
            }

            e.Handled = true;
        }

        /// <summary>
        /// Send mouse click event to the streaming PC
        /// </summary>
        private void MouseUp(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint ptrPt = e.GetCurrentPoint(StreamDisplay);

            if (e.Pointer.PointerDeviceType != PointerDeviceType.Mouse)
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
            else
            {
                // Send changes and update the current state
                int deltaButtons = mouseButtonFlag ^ GetButtonFlags(ptrPt);
                if ((deltaButtons & MOUSE_BUTTON_LEFT) != 0)
                {
                    LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Release,
                        (int)MouseButton.Left);
                }
                if ((deltaButtons & MOUSE_BUTTON_MIDDLE) != 0)
                {
                    LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Release,
                        (int)MouseButton.Middle);
                }
                if ((deltaButtons & MOUSE_BUTTON_RIGHT) != 0)
                {
                    LimelightCommonRuntimeComponent.SendMouseButtonEvent((byte)MouseButtonAction.Release,
                        (int)MouseButton.Right);
                }
                mouseButtonFlag = GetButtonFlags(ptrPt);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Send mouse move event to the streaming PC
        /// </summary>
        private void MouseMove(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint ptrPt = e.GetCurrentPoint(StreamDisplay);
            short eventX = (short)ptrPt.Position.X;
            short eventY = (short)ptrPt.Position.Y; 
            if (eventX != lastX || eventY != lastY)
            {
                hasMoved = true;
                short xToSend = (short)(eventX - lastX);
                short yToSend = (short)(eventY - lastY);
                // Send the values to the streaming PC so it can register mouse movement
                LimelightCommonRuntimeComponent.SendMouseMoveEvent(xToSend, yToSend);

                lastX = eventX;
                lastY = eventY;
            }
            
            // Prevent most handlers along the event route from handling the same event again.
            e.Handled = true;
        }
        #endregion Mouse Events

        #region Keyboard events

        private void WindowKeyDownHandler(CoreWindow sender, KeyEventArgs args)
        {
            short key = KeyboardHelper.TranslateVirtualKey(args.VirtualKey);
            if (key != 0)
            {
                LimelightCommonRuntimeComponent.SendKeyboardEvent(key, (byte)KeyAction.Down,
                    KeyboardHelper.GetModifierFlags());

                args.Handled = true;
            }
        }

        private void WindowKeyUpHandler(CoreWindow sender, KeyEventArgs args)
        {
            short key = KeyboardHelper.TranslateVirtualKey(args.VirtualKey);
            if (key != 0)
            {
                LimelightCommonRuntimeComponent.SendKeyboardEvent(key, (byte)KeyAction.Up,
                    KeyboardHelper.GetModifierFlags());

                args.Handled = true;
            }
        }

        #endregion
    } 
}
