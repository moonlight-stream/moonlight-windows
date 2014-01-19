using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;


namespace Limelight
{
    class H264FileReaderHackery
    {
        private StreamFrame frame;

        public H264FileReaderHackery(StreamFrame frame)
        {
            this.frame = frame;
        }


        public void readFile()
        {
            Debug.WriteLine("[H264FileReaderHackery::readFile] Get resource stream");
            var resourceStream  = Application.GetResourceStream(new Uri("Resources/notpadded.h264", UriKind.Relative));
            Debug.WriteLineIf(resourceStream == null, "Shit");
            var stream = resourceStream.Stream;
            Debug.WriteLine("[H264FileReaderHackery::readFile Creating byte[] buffer]");
            Byte[] buffer = new Byte[131072];
            int offset = 0;
            int seekOffset = 0;
            Debug.WriteLine("[H264FileReaderHackery::readFile Entering while(stream.CanRead]");

            while (stream.CanRead)
            {
                int len = stream.Read(buffer, 0, buffer.Length);
                if (len > 0) {
                    bool firstStart = false;
                    for (int i = 0; i < len - 4; i++) {
                        seekOffset++;
                        if (buffer[i] == 0 && buffer[i+1] == 0 && buffer[i+2] == 0 && buffer[i+3] == 1) {
                            if (firstStart) {
                                //we should decode the first i-1 
                                IBuffer ms = buffer.AsBuffer();
                                frame.videoStream.TransportController_VideoMessageReceived(ms,0,10);
                                stream.Position = --seekOffset;
                            } else {
                                firstStart = true;
                            }
                        } 
                    }
                } else {
                    Debug.WriteLine("[H264FileReaderHackery::readFile] No buffer");
                    stream.Position = 0;
                    seekOffset = 0;
                }
            }
        }
    }
}