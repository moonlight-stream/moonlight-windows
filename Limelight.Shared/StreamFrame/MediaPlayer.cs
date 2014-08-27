namespace Limelight
{
    using Limelight_common_binding;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Windows.Graphics.Display;
    using Windows.Media.Core;
    using Windows.Media.MediaProperties;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Navigation;
    public sealed partial class StreamFrame : Page
    {
        #region Class Variables
        private MediaStreamSource _mss = null;
        private VideoStreamDescriptor _videoDesc = null;
        private AudioStreamDescriptor _audioDesc = null;
        private AvStreamSource _streamSource;

        #endregion Class Variables

        #region Media Player

        /// <summary>
        /// Initialize the media element for playback
        /// </summary>
        /// <param name="streamConfig">Object containing stream configuration details</param>
        void InitializeMediaPlayer(LimelightStreamConfiguration streamConfig, AvStreamSource streamSource)
        {
            this._streamSource = streamSource;

            AudioEncodingProperties audioProperties = AudioEncodingProperties.CreatePcm(48000, 2, 16);

            VideoEncodingProperties videoProperties = VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.H264Es,
                (uint)streamConfig.GetWidth(), (uint)streamConfig.GetHeight());
            videoProperties.ProfileId = H264ProfileIds.High;

            _videoDesc = new VideoStreamDescriptor(videoProperties);
            _audioDesc = new AudioStreamDescriptor(audioProperties);

            _mss = new MediaStreamSource(_videoDesc, _audioDesc);
            _mss.BufferTime = TimeSpan.Zero;
            _mss.CanSeek = false;
            _mss.Duration = TimeSpan.Zero;
            _mss.SampleRequested += _mss_SampleRequested;

            // Set for low latency playback
            StreamDisplay.RealTimePlayback = true;

            // Set the audio category to take advantage of hardware audio offload
            StreamDisplay.AudioCategory = AudioCategory.ForegroundOnlyMedia;

            // Render on the full window to avoid extra compositing
            StreamDisplay.IsFullWindow = true;

            // Disable built-in transport controls
            StreamDisplay.AreTransportControlsEnabled = false;

            // Start playing right away
            StreamDisplay.AutoPlay = true;

            StreamDisplay.SetMediaStreamSource(_mss);
        }

        /// <summary>
        /// Media stream source sample requested callback
        /// </summary>
        private void _mss_SampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
        {
            // Determine which stream needs a sample
            if (args.Request.StreamDescriptor == _videoDesc)
            {
                // Video
                _streamSource.VideoSampleRequested(args);
            }
            else
            {
                // Audio
                _streamSource.AudioSampleRequested(args);
            }
        }
        #endregion Media Player
    }
}