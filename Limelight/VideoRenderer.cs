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
            /// <summary>
            /// Indicates if rendering is already in progress or not
            /// </summary>
            private bool isRendering;

            /// <summary>
            /// The stream source
            /// </summary>
            private VideoStreamSource mediaStreamSource;

            /// <summary>
            /// Provides streaming media for the MediaElement
            /// </summary>
            private MediaStreamer mediaStreamer; 

            /// <summary>
            /// Initializes a new instance of the <see cref="VideoRenderer"/> class. 
            /// </summary>
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
                    mediaStreamer = MediaStreamerFactory.CreateMediaStreamer(123);
                }
                                
                int frameWidth = (int)Application.Current.Host.Content.ActualWidth;
                int frameHeight = (int)Application.Current.Host.Content.ActualHeight;
                mediaStreamSource = new VideoStreamSource(null, frameWidth, frameHeight);
                mediaStreamer.SetSource(mediaStreamSource);                
            }
    }
}
