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
        /// Begins stream to the MediaElement
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
        }
        public void hacks(object o)
        {
            new H264FileReaderHackery(this).readFile();
        }
    }
}