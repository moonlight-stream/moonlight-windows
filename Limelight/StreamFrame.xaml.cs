using System.Windows;
using Microsoft.Phone.Controls;
using System.Threading; 


namespace Limelight
{
    public partial class StreamFrame : PhoneApplicationPage
    {
        int frameWidth = 1280;
        int frameHeight = 720;
        public VideoStreamSource videoStream;

        /// <summary>
        /// Begins stream to the MediaElement on the Application Frame
        /// </summary>
        public StreamFrame()
        {
            InitializeComponent();

            videoStream = new VideoStreamSource(null, frameWidth, frameHeight);
            StreamDisplay.SetSource(videoStream);  
            StreamDisplay.AutoPlay = true;
            StreamDisplay.Play();

            // Starts the demo video renderer in a new thread
            ThreadPool.QueueUserWorkItem(hacks);

            // TODO use Limelight Common to get the stream

            // TODO uncomment when you have a real stream to use
            //ThreadPool.QueueUserWorkItem(Decoder);

        }
        public void hacks(object o)
        {
            new H264FileReaderHackery(this).readFile();
        }

        /// <summary>
        /// Begins the decoder
        /// </summary>
        /// <param name="o"></param>
        public void Decoder(object o)
        {
            // TODO pass in resource stream
            new VideoDecoder(this).decodeVideo(null); 
        }
    }
}