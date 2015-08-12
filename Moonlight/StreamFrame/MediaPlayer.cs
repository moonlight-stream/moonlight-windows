namespace Moonlight
{
    using Moonlight_common_binding;
    using SharpDX.Multimedia;
    using SharpDX.XAudio2;
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
        private MediaStreamSource _videoMss = null;
        private AvStreamSource _streamSource;

        #endregion Class Variables

        #region Media Player

        /// <summary>
        /// Initialize the media element for playback
        /// </summary>
        /// <param name="streamConfig">Object containing stream configuration details</param>
        void InitializeMediaPlayer(MoonlightStreamConfiguration streamConfig, AvStreamSource streamSource)
        {
            this._streamSource = streamSource;

            // This code is based upon the MS FFmpegInterop project on GitHub
            VideoEncodingProperties videoProps = VideoEncodingProperties.CreateH264();
            videoProps.ProfileId = H264ProfileIds.High;
            videoProps.Width = (uint)streamConfig.GetWidth();
            videoProps.Height = (uint)streamConfig.GetHeight();
            videoProps.Bitrate = (uint)streamConfig.GetBitrate();

            _videoMss = new MediaStreamSource(new VideoStreamDescriptor(videoProps));
            _videoMss.BufferTime = TimeSpan.Zero;
            _videoMss.CanSeek = false;
            _videoMss.Duration = TimeSpan.Zero;
            _videoMss.SampleRequested += _videoMss_SampleRequested;

            XAudio2 xaudio = new XAudio2();
            MasteringVoice masteringVoice = new MasteringVoice(xaudio, 2, 48000);
            WaveFormat format = new WaveFormat(48000, 16, 2);

            // Set for low latency playback
            StreamDisplay.RealTimePlayback = true;

            // Render on the full window to avoid extra compositing
            StreamDisplay.IsFullWindow = true;

            // Disable built-in transport controls
            StreamDisplay.AreTransportControlsEnabled = false;

            StreamDisplay.SetMediaStreamSource(_videoMss);
            AvStream.SetSourceVoice(new SourceVoice(xaudio, format));
        }

        private void StartMediaPlayer()
        {
            AvStream.Start();
            StreamDisplay.Play();
        }

        private void StopMediaPlayer()
        {
            AvStream.Stop();
            StreamDisplay.Stop();
        }

        /// <summary>
        /// Video stream source sample requested callback
        /// </summary>
        private void _videoMss_SampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
        {
            _streamSource.VideoSampleRequested(args);
        }
        #endregion Media Player
    }
}