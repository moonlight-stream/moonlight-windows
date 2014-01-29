namespace Limelight
{
    using System.Net;
    using System.Threading;
    using System.Windows;
    using Limelight_common_binding;
    using Microsoft.Phone.Controls;
    using System.Diagnostics;

    /// <summary>
    /// UI Frame that contains the media element that streams Steam
    /// </summary>
    public partial class StreamFrame : PhoneApplicationPage
    {
        // TODO make these not magic numbers!! :(

        /// <summary>
        /// Width of the frame from the video source
        /// </summary>
        private int frameWidth = 1280;

        /// <summary>
        /// Height of the frame from the video source
        /// </summary>
        private int frameHeight = 720;

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

            // TODO uncomment when you have a real stream to use
            ThreadPool.QueueUserWorkItem(Connection);
        }

        /// <summary>
        /// Begins the demo video stream
        /// </summary>
        /// <param name="o">Object for the thread</param>
        public void Hacks(object o)
        {
            new H264FileReaderHackery(this).readFile();
        }

        /// <summary>
        /// Starts the connection
        /// </summary>
        /// <param name="o">Object for the connection thread</param>
        public void Connection(object o)
        {
            LimelightStreamConfiguration streamConfig = new LimelightStreamConfiguration(1280, 720, 30);

            Debug.WriteLine("Starting connection\n");
            LimelightCommonRuntimeComponent.StartConnection((uint)IPAddress.HostToNetworkOrder((int)IPAddress.Parse("129.22.46.110").Address), streamConfig);
        }
    }
}