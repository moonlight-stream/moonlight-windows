using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;


namespace Limelight
{
    class H264FileReaderHackery
    {
        private StreamFrame frame;

        public H264FileReaderHackery(StreamFrame frame)
        {
            this.frame = frame;
        }


        public async void readFile()
        {
            var resourceStream  = App.GetResourceStream(new Uri("notpadded.h264", UriKind.Relative));
            var stream = resourceStream.Stream;
            Byte[] buffer = new Byte[131072];
            int offset = 0;
            int seekOffset = 0;
            while (stream.CanRead)
            {
                int len = stream.Read(buffer, offset, buffer.Length);
                if (len > 0) {
                    bool firstStart = false;
                    for (int i = 0; i < len - 4; i++) {
                        seekOffset++;
                        if (buffer[i] == 0 && buffer[i+1] == 0 && buffer[i+2] == 0 && buffer[i+3] == 1) {
                            if (firstStart) {
                                //we should decode the first i-1 bytes
                                //TODO: ^ that
                                IBuffer ms = buffer.AsBuffer(); 
                                frame.videoStream.TransportController_VideoMessageReceived(ms,0,10);
                                offset = --seekOffset;
                            } else {
                                firstStart = true;
                            }
                        } 
                    }
                } else {
                    Debug.WriteLine("No buffer");
                }
            }
        }
    }
    /*len = [(NSInputStream *)inStream read:self.byteBuffer maxLength:BUFFER_LENGTH];
        if (len)
        {
            BOOL firstStart = false;
            for (int i = 0; i < len - 4; i++) {
                self.offset++;
                if (self.byteBuffer[i] == 0 && self.byteBuffer[i+1] == 0
                    && self.byteBuffer[i+2] == 0 && self.byteBuffer[i+3] == 1)
                {
                    if (firstStart)
                    {
                        // decode the first i-1 bytes and render a frame
                        [self.decoder decode:self.byteBuffer length:i];
                        [self.target performSelectorOnMainThread:@selector(setNeedsDisplay) withObject:NULL waitUntilDone:FALSE];
                        
                        // move offset back to beginning of start sequence
                        [inStream setProperty:[[NSNumber alloc] initWithInt:self.offset-4] forKey:NSStreamFileCurrentOffsetKey];
                        self.offset -= 1;
                        
                        break;
                    } else
                    {
                        firstStart = true;
                    }
                }
            }
        }
        else
        {
            NSLog(@"No Buffer!");
        }
    *