namespace Limelight
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading;
    using System.Windows.Media;
    using Windows.Storage.Streams;

    /// <summary>
    /// Custom source for a video stream
    /// </summary>
    public class AvStreamSource : MediaStreamSource, IDisposable
    {
        /// <summary>
        /// Audio Sample object
        /// </summary>
        public class AudioSample
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="AudioSample"/> class. 
            /// </summary>
            /// <param name="buffer">Buffer for the audio sample</param>
            public AudioSample(IBuffer buffer)
            {
                this.buffer = buffer;
            }
            internal IBuffer buffer { get; private set; }
        }

        /// <summary>
        /// Video Sample object
        /// </summary>
        public class VideoSample
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="VideoSample"/> class. 
            /// </summary>
            /// <param name="buffer">Buffer for the video stream</param>
            /// <param name="frameNumber">The frame number that this sample is part of</param>
            public VideoSample(IBuffer buffer, ulong frameNumber)
            {
                this.buffer = buffer;
                this.frameNumber = frameNumber;
            }
            internal IBuffer buffer { get; private set; }
            internal ulong frameNumber { get; private set; }
        }

        private int frameWidth;
        private int frameHeight;
        private object lockObj = new object();
        private ManualResetEvent shutdownEvent;
        private Dictionary<MediaSampleAttributeKeys, string> emptySampleDict = new Dictionary<MediaSampleAttributeKeys, string>();
        private ulong frameNumber;

        private volatile int outstandingGetVideoSampleCount;
        private volatile int outstandingGetAudioSampleCount;
        private MediaStreamDescription videoDesc;
        private MediaStreamDescription audioDesc;
        private Queue<VideoSample> nalQueue;
        private Queue<AudioSample> audioQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvStreamSource"/> class. 
        /// </summary>
        /// <param name="frameWidth">Width of the source's video frame</param>
        /// <param name="frameHeight">Height of the source's video frame</param>
        public AvStreamSource(int frameWidth, int frameHeight)
        {
            this.frameWidth = frameWidth;
            this.frameHeight = frameHeight;
            this.shutdownEvent = new ManualResetEvent(false);
            this.nalQueue = new Queue<VideoSample>();

            // 15 is the minimum size
            this.AudioBufferLength = 15;
        }

        /// <summary>
        /// Shuts down the video stream
        /// </summary>
        public void Shutdown()
        {
            shutdownEvent.Set();
            if (outstandingGetVideoSampleCount > 0)
            {
                lock (lockObj)
                {
                    // ReportGetSampleCompleted must be called after GetSampleAsync to avoid memory leak. So, send
                    // an empty MediaStreamSample here.
                    MediaStreamSample mediaStreamSamp = new MediaStreamSample(
                        videoDesc, null, 0, 0, 0, emptySampleDict);
                    ReportGetSampleCompleted(mediaStreamSamp);
                    outstandingGetVideoSampleCount = 0;
                }
            }
            if (outstandingGetAudioSampleCount > 0)
            {
                lock (lockObj)
                {
                    // ReportGetSampleCompleted must be called after GetSampleAsync to avoid memory leak. So, send
                    // an empty MediaStreamSample here.
                    MediaStreamSample mediaStreamSamp = new MediaStreamSample(
                        audioDesc, null, 0, 0, 0, emptySampleDict);
                    ReportGetSampleCompleted(mediaStreamSamp);
                    outstandingGetAudioSampleCount = 0;
                }
            }
        }

        private void EnqueueNal(byte[] buf, int nalStart, int nalEnd, ulong frameNumber)
        {
            int nalLength = nalEnd - nalStart;
            byte[] nal = new byte[nalLength];
            Array.ConstrainedCopy(buf, nalStart, nal, 0, nalLength);
            lock (lockObj)
            {
                nalQueue.Enqueue(new VideoSample(nal.AsBuffer(), frameNumber));
            }
        }

        /// <summary>
        /// Enqueues next video samples in the buffer
        /// </summary>
        /// <param name="buf">Buffer for the video stream</param>
        public void EnqueueVideoSamples(byte[] buf)
        {
            int i;

            int currentNalStart = -1;
            bool frameStart = false;
            for (i = 0; i < buf.Length - 4; i++)
            {
                // Look for the Annex B NAL start sequence (0x000001)
                if (buf[i] == 0 && buf[i+1] == 0)
                {
                    // Check for frame start
                    if (buf[i + 2] == 0 && buf[i + 3] == 1)
                    {
                        // We're on the next frame
                        frameNumber++;

                        // Remember this is a frame start NAL for later
                        frameStart = true;
                    }
                    else if (buf[i + 2] == 1)
                    {
                        // NAL start (but not frame start)
                        frameStart = false;
                    }
                    else
                    {
                        // Not actually NAL start
                        continue;
                    }

                    // End the current NAL at i (exclusive)
                    if (currentNalStart > 0)
                    {
                        EnqueueNal(buf, currentNalStart, i, frameNumber);
                    }

                    if (frameStart)
                    {
                        // Skip the first zero byte of the 4-byte frame start prefix
                        i++;
                    }

                    // NAL start
                    currentNalStart = i;
                }
            }

            // Add the NAL that ends at the buffer's end
            if (currentNalStart > 0)
            {
                EnqueueNal(buf, currentNalStart, buf.Length, frameNumber);
            }

            SendVideoSamples();
        }

        /// <summary>
        /// Enqueues next audio samples in the buffer
        /// </summary>
        /// <param name="buf">Buffer for the audio stream</param>
        public void EnqueueAudioSamples(byte[] buf)
        {
            audioQueue.Enqueue(new AudioSample(buf.AsBuffer()));
            SendAudioSamples();
        }

        /// <summary>
        /// Dequeues audio stream samples and sends them to the renderer
        /// </summary>
        private void SendAudioSamples()
        {
            // If shutdown event is pending, return
            if (shutdownEvent.WaitOne(0))
            {
                return;
            }

            while (true)
            {
                AudioSample audioSample;

                if (outstandingGetAudioSampleCount == 0)
                {
                    return;
                }

                lock (lockObj)
                {
                    if (audioQueue.Count() == 0)
                    {
                        return;
                    }

                    audioSample = audioQueue.Dequeue();
                }

                Stream sampleStream = WindowsRuntimeBufferExtensions.AsStream(audioSample.buffer);

                // Send out the next LPCM sample
                MediaStreamSample mediaStreamSamp = new MediaStreamSample(
                    audioDesc,
                    sampleStream,
                    0,
                    sampleStream.Length,
                    (long)frameNumber, // FIXME?
                    emptySampleDict);

                ReportGetSampleCompleted(mediaStreamSamp);
                outstandingGetAudioSampleCount--;
            }
        }

        /// <summary>
        /// Dequeues video stream samples and sends them to the renderer
        /// </summary>
        private void SendVideoSamples()
        {
            // If shutdown event is pending, return
            if (shutdownEvent.WaitOne(0))
            {
                return;
            }

            while (true)
            {
                VideoSample videoSample;

                if (outstandingGetVideoSampleCount == 0)
                {
                    return;
                }

                lock (lockObj)
                {
                    if (nalQueue.Count() % 10 == 0)
                    {
                        Debug.WriteLine("Queued NALs: " + nalQueue.Count());
                    }

                    if (nalQueue.Count() == 0)
                    {
                        return;
                    }

                    videoSample = nalQueue.Dequeue();
                }

                Stream sampleStream = WindowsRuntimeBufferExtensions.AsStream(videoSample.buffer);

                // Send out the next NAL
                MediaStreamSample mediaStreamSamp = new MediaStreamSample(
                    videoDesc,
                    sampleStream,
                    0,
                    sampleStream.Length,
                    (long)videoSample.frameNumber,
                    emptySampleDict);

                ReportGetSampleCompleted(mediaStreamSamp);
                outstandingGetVideoSampleCount--;
            }
        }

        /// <summary>
        /// Prepare the video stream for rendering
        /// </summary>
        private void PrepareVideo()
        {
            // Stream Description 
            Dictionary<MediaStreamAttributeKeys, string> streamAttributes =
                new Dictionary<MediaStreamAttributeKeys, string>();

            // Select the same encoding and dimensions as the video capture
            streamAttributes[MediaStreamAttributeKeys.VideoFourCC] = "H264";
            streamAttributes[MediaStreamAttributeKeys.Height] = frameHeight.ToString();
            streamAttributes[MediaStreamAttributeKeys.Width] = frameWidth.ToString();

            MediaStreamDescription msd =
                new MediaStreamDescription(MediaStreamType.Video, streamAttributes);

            videoDesc = msd;
        }

        private string ByteToBase16Str(byte inputByte)
        {
            return inputByte.ToString("X2");
        }

        private string ShortToBase16Str(short inputShort)
        {
            byte[] bytes = new byte[2];
            bytes[0] = (byte)(inputShort & 0xFF);
            bytes[1] = (byte)(inputShort >> 8);
            return ByteToBase16Str(bytes[0]) + ByteToBase16Str(bytes[1]);
        }

        private string IntToBase16Str(int inputInt)
        {
            byte[] bytes = new byte[4];

            bytes[0] = (byte)(inputInt & 0xFF);
            bytes[1] = (byte)(inputInt >> 8);
            bytes[2] = (byte)(inputInt >> 16);
            bytes[3] = (byte)(inputInt >> 24);
            return ByteToBase16Str(bytes[0]) + ByteToBase16Str(bytes[1]) +
                ByteToBase16Str(bytes[2]) + ByteToBase16Str(bytes[3]);
        }

        /// <summary>
        /// Prepare the audio stream 
        /// </summary>
        private void PrepareAudio()
        {
            // Stream Description 
            Dictionary<MediaStreamAttributeKeys, string> streamAttributes =
                new Dictionary<MediaStreamAttributeKeys, string>();

            streamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = 
                ShortToBase16Str(1) + // wFormatTag = WAVE_FORMAT_PCM
                ShortToBase16Str(2) + // nChannels = 2
                IntToBase16Str(48000) + // nSamplesPerSec = 48000
                IntToBase16Str(192000) + // nAvgBytesPerSec = (nSamplesPerSec * nBlockAlign) = 192000
                ShortToBase16Str(4) + // nBlockAlign = (nChannels * wBitsPerSample) / 8 = 4
                ShortToBase16Str(16) + // wBitsPerSample = 16
                ShortToBase16Str(0); // cbSize = 0

            MediaStreamDescription msd =
                new MediaStreamDescription(MediaStreamType.Audio, streamAttributes);

            audioDesc = msd;
        }

        /// <summary>
        /// Performs asynchronous streaming of the media
        /// </summary>
        protected override void OpenMediaAsync()
        {
            // Init
            Dictionary<MediaSourceAttributesKeys, string> sourceAttributes =
                new Dictionary<MediaSourceAttributesKeys, string>();
            List<MediaStreamDescription> availableStreams =
                new List<MediaStreamDescription>();

            // Set attributes and list our stream as available
            PrepareVideo();
            PrepareAudio();
            availableStreams.Add(videoDesc);
            availableStreams.Add(audioDesc);

            // A zero timespan is an infinite stream
            sourceAttributes[MediaSourceAttributesKeys.Duration] =
                TimeSpan.FromSeconds(0).Ticks.ToString(CultureInfo.InvariantCulture);

            sourceAttributes[MediaSourceAttributesKeys.CanSeek] = false.ToString();

            // Tell Silverlight that we've prepared and opened our AV stream
            ReportOpenMediaCompleted(sourceAttributes, availableStreams);
        }

        /// <summary>
        /// Gets the samples from the video and audio streams
        /// </summary>
        /// <param name="mediaStreamType">Audio or video stream</param>
        protected override void GetSampleAsync(MediaStreamType mediaStreamType)
        {
            if (mediaStreamType == MediaStreamType.Audio)
            {
                outstandingGetAudioSampleCount++;
            }
            else if (mediaStreamType == MediaStreamType.Video)
            {
                outstandingGetVideoSampleCount++;
            }
        }
        
        /// <summary>
        /// The MediaElement can call this method when going through normal shutdown or as a result of an error
        /// </summary>
        protected override void CloseMedia()
        {
            // Nothing to do
        }

        /// <summary>
        /// The MediaElement can call this method to request information about the MediaStreamSource
        /// </summary>
        /// <param name="diagnosticKind">Describes the type of diagnostic information used by the media</param>
        protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when a stream switch is requested on the MediaElement
        /// </summary>
        /// <param name="mediaStreamDescription">Describes a media stream</param>
        protected override void SwitchMediaStreamAsync(MediaStreamDescription mediaStreamDescription)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The MediaElement calls this method to ask the MediaStreamSource to seek to the nearest randomly accessible point before the specified time
        /// </summary>
        /// <param name="seekToTime">The time as represented by 100 nanosecond increments to seek to</param>
        protected override void SeekAsync(long seekToTime)
        {
            Debug.WriteLine("Seeking to " + seekToTime);
            ReportSeekCompleted(seekToTime); 
        }

        protected virtual void Dispose(bool managed)
        {
            if (managed)
            {
                shutdownEvent.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
