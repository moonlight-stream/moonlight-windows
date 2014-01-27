namespace Limelight
{
    using System.Threading;
    using System.Windows;
    using Microsoft.Phone.Controls;

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

        // TODO Uncomment when Cameron has Limelight-common-c up and running
        // LimelightStreamConfiguration c;

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

            // Starts the demo video renderer in a new thread
            ThreadPool.QueueUserWorkItem(Hacks);

            // TODO use Limelight Common to get the stream

            // TODO uncomment when you have a real stream to use
            // ThreadPool.QueueUserWorkItem(Decoder);
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
        /// Begins the decoder
        /// </summary>
        /// <param name="o">Object for the decoder thread</param>
        public void Decoder(object o)
        {
            // TODO pass in resource stream
            new VideoDecoder(this).DecodeVideo(null); 
        }
    }
}