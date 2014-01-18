using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Limelight
{
    using Microsoft.Phone.Media;
    using System;
    using System.Diagnostics;
    using System.Windows;

    namespace LimeSpace
    {
        /// <summary>
        /// A class that renders video from the background process.
        /// Note, the MediaElement that actually displays the video is in the UI process - 
        /// this class receives video from the remote party and writes it to a media streamer.
        /// The media streamer handles connecting the rendered video stream to the media element that
        /// displays it in the UI process.
        /// </summary>
        internal class VideoRenderer
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal VideoRenderer()
            {
            }

            #region IVideoRenderer methods

            /// <summary>
            /// Start rendering video.
            /// Note, this method may be called multiple times in a row.
            /// </summary>
            public void Start()
            {
                if (this.isRendering)
                    return; // Nothing more to be done

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

            private void StartMediaStreamer()
            {
                if (mediaStreamer == null)
                {
                    mediaStreamer = MediaStreamerFactory.CreateMediaStreamer(123);
                }

                
                int frameWidth = (int)Application.Current.Host.Content.ActualWidth;
                int frameHeight = (int)Application.Current.Host.Content.ActualHeight;
                mediaStreamSource = new VideoStreamSource(null, frameWidth, frameHeight);
                mediaStreamer.SetSource(mediaStreamSource);
            }

            /// <summary>
            /// Stop rendering video.
            /// Note, this method may be called multiple times in a row.
            /// </summary>
            public void Stop()
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (!this.isRendering)
                        return; // Nothing more to be done

                    Debug.WriteLine("[Video Renderer] Video rendering stopped.");
                    //mediaStreamSource.Shutdown();
                    //mediaStreamSource.Dispose();
                    mediaStreamSource = null;
                    mediaStreamer.Dispose();
                    mediaStreamer = null;

                    this.isRendering = false;
                });
            }

            #endregion

            #region Private members

            // Indicates if rendering is already in progress or not
            private bool isRendering;
            private VideoStreamSource mediaStreamSource;
            private MediaStreamer mediaStreamer;

            #endregion
        }
    }
}
