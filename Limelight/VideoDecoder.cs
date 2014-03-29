namespace Limelight
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Windows.Resources;
    using Windows.Storage.Streams;
    //using Limelight_common_binding;
    using System.Net;

    /// <summary>
    /// Video Decoder class
    /// </summary>
    internal class VideoDecoder
    {
        private StreamFrame frame;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoDecoder"/> class.
        /// </summary>
        /// <param name="frame">The UI frame</param>
        public VideoDecoder(StreamFrame frame)
        {
            this.frame = frame;
        }

        /// <summary>
        /// Decodes the H264 video stream for rendering
        /// </summary>
        /// <param name="resourceStream">The resource stream for the video</param>
        public void DecodeVideo(StreamResourceInfo resourceStream)
        {
            Debug.WriteLine("[VideoDecoder] Decoding video...");

            Stream stream = resourceStream.Stream;
            IBuffer mediaStream;
            int seekOffset = 0;

            const ulong sampleDuration = 10; 

            // 128 kilobyte byte buffer
            byte[] buffer = new byte[131072];

            while (stream.CanRead)
            {
                int len = stream.Read(buffer, 0, buffer.Length);
                if (len > 0)
                {
                    bool firstStart = false;
                    for (int i = 0; i < len - 4; i++)
                    {
                        seekOffset++;
                        if (buffer[i] == 0 && buffer[i + 1] == 0 && buffer[i + 2] == 0 && buffer[i + 3] == 1)
                        {
                            if (firstStart)
                            {
                                // Put the buffer contents into a format that the video stream source can use
                                mediaStream = buffer.AsBuffer();

                                frame.VideoStream.EnqueueSamples(mediaStream, 0, sampleDuration);
                                stream.Position = --seekOffset;
                            }
                            else
                            {
                                firstStart = true;
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("[VideoDecoder] No buffer");
                }
            }
        }
    }
}
