using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace Limelight
{
    public class VideoStreamSource : MediaStreamSource
    {
        //private Stream _videoStream;
        //private Stream _audioStream;
        //private WaveFormatEx _waveFormat;
        private byte[] _audioSourceBytes;
        private long _currentAudioTimeStamp;

        private MediaStreamDescription _audioDesc;

        private Stream _frameStream;

        private int _frameWidth;
        private int _frameHeight;

        private int _framePixelSize;
        private int _frameBufferSize;
        public const int BytesPerPixel = 4;   // 32 bit including alpha


        private byte[][] _frames = new byte[2][];

        private int _currentReadyFrame;
        private int _currentBufferFrame;


        private int _frameTime;
        private long _currentVideoTimeStamp;

        private MediaStreamDescription _videoDesc;
        private Dictionary<MediaSampleAttributeKeys, string> _emptySampleDict = new Dictionary<MediaSampleAttributeKeys, string>();


        //public byte[] CurrentFrameBytes
        //{
        //    get { return _frame; }
        //}


        public void WritePixel(int position, Color color)
        {
            //BitConverter.GetBytes(color).CopyTo(_frame, position * BytesPerPixel);

            int offset = position * BytesPerPixel;

            _frames[_currentBufferFrame][offset++] = color.B;
            _frames[_currentBufferFrame][offset++] = color.G;
            _frames[_currentBufferFrame][offset++] = color.R;
            _frames[_currentBufferFrame][offset++] = color.A;

            //if (position < 10)
            //    System.Diagnostics.Debug.WriteLine("Pixel at {3} is {0} {1} {2}", color.R, color.B, color.G, position);
        }



        public VideoStreamSource(int frameWidth, int frameHeight)
        {
           // _audioStream = audioStream;

            _frameWidth = frameWidth;
            _frameHeight = frameHeight;

            _framePixelSize = frameWidth * frameHeight;
            _frameBufferSize = _framePixelSize * BytesPerPixel;

            // PAL is 50 frames per second
            _frameTime = (int)TimeSpan.FromSeconds((double)1 / 50).Ticks;

            _frames[0] = new byte[_frameBufferSize];
            _frames[1] = new byte[_frameBufferSize];

            _currentBufferFrame = 0;
            _currentReadyFrame = 1;
        }


        public void Flip()
        {
            int f = _currentBufferFrame;
            _currentBufferFrame = _currentReadyFrame;
            _currentReadyFrame = f;
        }

        private void PrepareVideo()
        {
            _frameStream = new MemoryStream();

        http://open.spotify.com/track/3xDVJcvcKedshWlT3qGSHk    // Stream Description 
            Dictionary<MediaStreamAttributeKeys, string> streamAttributes =
                new Dictionary<MediaStreamAttributeKeys, string>();

            streamAttributes[MediaStreamAttributeKeys.VideoFourCC] = "RGBA";
            streamAttributes[MediaStreamAttributeKeys.Height] = _frameHeight.ToString();
            streamAttributes[MediaStreamAttributeKeys.Width] = _frameWidth.ToString();

            MediaStreamDescription msd =
                new MediaStreamDescription(MediaStreamType.Video, streamAttributes);

            _videoDesc = msd;
        }


      /*  private void PrepareAudio()
        {
            short BitsPerSample = 16;
            int SampleRate = 8000;          // change this to something higher if we output sound from here
            short ChannelCount = 1;
            int ByteRate = SampleRate * ChannelCount * (BitsPerSample / 8);


            _waveFormat = new WaveFormatEx();
            _waveFormat.BitsPerSample = BitsPerSample;
            _waveFormat.AvgBytesPerSec = (int)ByteRate;
            _waveFormat.Channels = ChannelCount;
            _waveFormat.BlockAlign = (short)(ChannelCount * (BitsPerSample / 8));
            _waveFormat.ext = null; // ??
            _waveFormat.FormatTag = WaveFormatEx.FormatPCM;
            _waveFormat.SamplesPerSec = SampleRate;
            _waveFormat.Size = 0; // must be zero

            _waveFormat.ValidateWaveFormat();


            _audioStream = new System.IO.MemoryStream();
            _audioSourceBytes = new byte[ByteRate];


            // TEMP just load the audio buffer with silence
            for (int i = 1; i < SampleRate; i++)
            {
                _audioSourceBytes[i] = 0;
            }

            // Stream Description 
            Dictionary<MediaStreamAttributeKeys, string> streamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
            streamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = _waveFormat.ToHexString(); // wfx
            MediaStreamDescription msd = new MediaStreamDescription(MediaStreamType.Audio, streamAttributes);
            _audioDesc = msd;

        }*/

        protected override void OpenMediaAsync()
        {
            // Init
            Dictionary<MediaSourceAttributesKeys, string> sourceAttributes =
                new Dictionary<MediaSourceAttributesKeys, string>();
            List<MediaStreamDescription> availableStreams =
                new List<MediaStreamDescription>();

            PrepareVideo();

            availableStreams.Add(_videoDesc);

            // a zero timespan is an infinite video
            sourceAttributes[MediaSourceAttributesKeys.Duration] =
                TimeSpan.FromSeconds(0).Ticks.ToString(CultureInfo.InvariantCulture);

            sourceAttributes[MediaSourceAttributesKeys.CanSeek] = false.ToString();

            // tell Silverlight that we've prepared and opened our video
            ReportOpenMediaCompleted(sourceAttributes, availableStreams);
        }

        protected override void GetSampleAsync(MediaStreamType mediaStreamType)
        {
            /*if (mediaStreamType == MediaStreamType.Audio)
            {
                GetAudioSample();
            }*/
            if (mediaStreamType == MediaStreamType.Video)
            {
                GetVideoSample();
            }
        }


        // TEMP!
        /*private void GetAudioSample()
        {
            int bufferSize = _audioSourceBytes.Length;

            // spit out one second
            _audioStream.Write(_audioSourceBytes, 0, bufferSize);

            // Send out the next sample
            MediaStreamSample msSamp = new MediaStreamSample(
                _audioDesc,
                _audioStream,
                0,
                bufferSize,
                _currentAudioTimeStamp,
                _emptySampleDict);

            _currentAudioTimeStamp += _waveFormat.AudioDurationFromBufferSize((uint)bufferSize);

            ReportGetSampleCompleted(msSamp);
        }*/

        //private static int offset = 0;
        private void GetVideoSample()
        {
            // seems like creating a new stream is only way to avoid out of memory and
            // actually figure out the correct offset. that can't be right.
            _frameStream = new MemoryStream();
            _frameStream.Write(_frames[_currentReadyFrame], 0, _frameBufferSize);

            // Send out the next sample
            MediaStreamSample msSamp = new MediaStreamSample(
                _videoDesc,
                _frameStream,
                0,
                _frameBufferSize,
                _currentVideoTimeStamp,
                _emptySampleDict);

            _currentVideoTimeStamp += _frameTime;

            ReportGetSampleCompleted(msSamp);
        }

        protected override void CloseMedia()
        {
            _currentAudioTimeStamp = 0;
            _currentVideoTimeStamp = 0;
        }

        protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind)
        {
            throw new NotImplementedException();
        }

        protected override void SwitchMediaStreamAsync(MediaStreamDescription mediaStreamDescription)
        {
            throw new NotImplementedException();
        }

        protected override void SeekAsync(long seekToTime)
        {
            _currentVideoTimeStamp = seekToTime;

            ReportSeekCompleted(seekToTime);
        }


    }
}
