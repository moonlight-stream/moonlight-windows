using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using Windows.Storage.Streams;

namespace Limelight
{
    class VideoDecoder
    {
        private StreamFrame frame;
        public VideoDecoder(StreamFrame frame)
        {
            this.frame = frame;
        }

        /// <summary>
        /// Decodes the H264 video stream for rendering
        /// </summary>
        /// <param name="resourceStream">The resource stream for the video</param>
        public void decodeVideo(StreamResourceInfo resourceStream)
        {
            Debug.WriteLine("[VideoDecoder] Decoding video...");

            Stream stream = resourceStream.Stream;
            IBuffer mediaStream;
            int seekOffset = 0;

            // 128 kilobyte buffer
            Byte[] buffer = new Byte[131072];

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

                                frame.videoStream.TransportController_VideoMessageReceived(mediaStream, 0, 10);
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

                    // TODO When reading the file, resetting these make it loop the video
                    // The decoder isn't for reading from a file
                    stream.Position = 0;
                    seekOffset = 0;
                }
            }
        }
    }
}
