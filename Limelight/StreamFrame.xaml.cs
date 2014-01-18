using System.Windows;
using Microsoft.Phone.Controls;


namespace Limelight
{
    public partial class StreamFrame : PhoneApplicationPage
    {
        int frameWidth = (int)Application.Current.Host.Content.ActualWidth;
        int frameHeight = (int)Application.Current.Host.Content.ActualHeight;

        public StreamFrame()
        {
            VideoStreamSource videoStream = new VideoStreamSource(null, frameWidth, frameHeight);
            InitializeComponent();
            StreamDisplay.SetSource(videoStream);
            StreamDisplay.AutoPlay = true;
            StreamDisplay.Play();
            
        }
    }
}