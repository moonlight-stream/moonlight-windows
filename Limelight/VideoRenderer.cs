namespace Limelight
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using Microsoft.Phone.Media;

        /// <summary>
        /// Renders video from the stream
        /// </summary>
        internal class VideoRenderer
        {
            private const int streamID = 123; 

            private bool isRendering;
            private VideoStreamSource mediaStreamSource;
            private MediaStreamer mediaStreamer; 

            internal VideoRenderer()
            {
            }

            /// <summary>
            /// Start rendering video.
            /// </summary>
            public void Start()
            {
                Debug.WriteLine("[VideoRenderer::Start");

                if (this.isRendering)
                {
                    return; // Nothing more to be done
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        Debug.WriteLine("[VideoRenderer::Start] Video rendering setup");
                        StartMediaStreamer();
                        this.isRendering = true;
                    }
                    catch (Exception err)
                    {
                        Debug.WriteLine("[VideoRenderer::Start] " + err.Message);
                    }
                });
            }

            /// <summary>
            /// Stop rendering video.
            /// </summary>
            public void Stop()
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (!this.isRendering)
                    {
                        return; // Nothing more to be done
                    }

                    Debug.WriteLine("[VideoRenderer::Stop] Video rendering stopped.");
                    mediaStreamSource = null;
                    mediaStreamer.Dispose();
                    mediaStreamer = null;

                    this.isRendering = false;
                });
            }

            /// <summary>
            /// Starts the media streamer
            /// </summary>
            private void StartMediaStreamer()
            {
                Debug.WriteLine("[VideoRenderer::StartMediaStreamer]");

                if (mediaStreamer == null)
                {
                    Debug.WriteLine("[VideoRenderer::StartMediaStreamer] CreateMediaStreamer");
                    mediaStreamer = MediaStreamerFactory.CreateMediaStreamer(streamID);
                }
                                
                int frameWidth = (int)Application.Current.Host.Content.ActualWidth;
                int frameHeight = (int)Application.Current.Host.Content.ActualHeight;
                mediaStreamSource = new VideoStreamSource(null, frameWidth, frameHeight);
                mediaStreamer.SetSource(mediaStreamSource);                
            }
    }
}
