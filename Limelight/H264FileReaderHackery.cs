using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

namespace Limelight
{
    class H264FileReaderHackery
    {
        public async Task readFile(StreamFrame frame)
        {
            Debug.WriteLine("[H264FileReaderHackery::readFile] Get resource stream");
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("notpadded.h264");
            Stream stream = await file.OpenStreamForReadAsync(); 
            
            Debug.WriteLine("[H264FileReaderHackery::readFile Creating byte[] buffer]");
            Byte[] buffer = new Byte[131072];
            int offset = 0;
            int seekOffset = 0;
            Debug.WriteLine("[H264FileReaderHackery::readFile Entering while(stream.CanRead]");

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
                                //we should decode the first i-1
                                byte[] frameData = new byte[i];
                                Array.ConstrainedCopy(buffer, 0, frameData, 0, i);
                                frame.AvStream.EnqueueVideoSample(frameData);

                                await Task.Delay(33);

                                stream.Position = --seekOffset;

                                break;
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
                    Debug.WriteLine("[H264FileReaderHackery::readFile] No buffer");
                    stream.Position = 0;
                    seekOffset = 0;
                }
            }
        }
    }
}
