using System.Windows;
using Microsoft.Phone.Controls;


namespace Limelight
{
    public partial class StreamFrame : PhoneApplicationPage
    {
        int frameWidth = 1280;//(int)Application.Current.Host.Content.ActualWidth;
        int frameHeight = 720;//(int)Application.Current.Host.Content.ActualHeight;
        public VideoStreamSource videoStream;
        public StreamFrame()
        {
            InitializeComponent();
            videoStream = new VideoStreamSource(null, frameWidth, frameHeight);
            StreamDisplay.SetSource(videoStream);
            StreamDisplay.AutoPlay = true;
            StreamDisplay.Play();
            
        }
    }
}