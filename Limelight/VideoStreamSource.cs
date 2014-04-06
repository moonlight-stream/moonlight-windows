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
    public class VideoStreamSource : MediaStreamSource
    {
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
        private Queue<VideoSample> nalQueue;
        private object lockObj = new object();
        private ManualResetEvent shutdownEvent;
        private volatile int outstandingGetVideoSampleCount;
        private MediaStreamDescription videoDesc;
        private Dictionary<MediaSampleAttributeKeys, string> emptySampleDict = new Dictionary<MediaSampleAttributeKeys, string>();
        private ulong frameNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoStreamSource"/> class. 
        /// </summary>
        /// <param name="audioStream">Audio stream associated with the video stream</param>
        /// <param name="frameWidth">Width of the source's video frame</param>
        /// <param name="frameHeight">Height of the source's video frame</param>
        public VideoStreamSource(Stream audioStream, int frameWidth, int frameHeight)
        {
            this.frameWidth = frameWidth;
            this.frameHeight = frameHeight;
            this.shutdownEvent = new ManualResetEvent(false);
            this.nalQueue = new Queue<VideoSample>();
            this.outstandingGetVideoSampleCount = 0;

            // 15 is the minimum size
            this.AudioBufferLength = 15;
        }

        /// <summary>
        /// Shuts down the video stream
        /// </summary>
        public void Shutdown()
        {
            Debug.WriteLine("[VideoStreamSource::Shutdown]");
            shutdownEvent.Set();
            if (outstandingGetVideoSampleCount > 0)
            {
                lock (lockObj)
                {
                    // ReportGetSampleCompleted must be called after GetSampleAsync to avoid memory leak. So, send
                    // an empty MediaStreamSample here.
                    MediaStreamSample mediaStreamSamp = new MediaStreamSample(
                        videoDesc, null, 0, 0, 0, 0, emptySampleDict);
                    ReportGetSampleCompleted(mediaStreamSamp);
                    outstandingGetVideoSampleCount = 0;
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
        public void EnqueueSamples(byte[] buf)
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

            SendSamples();
        }

        /// <summary>
        /// Dequeues media stream samples and sends them to the renderer
        /// </summary>
        private void SendSamples()
        {
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
                    0,
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
            Debug.WriteLine("[VideoStreamSource::PrepareVideo]");

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

        /// <summary>
        /// Prepare the audio stream 
        /// </summary>
        private void PrepareAudio()
        {
            // TODO 
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs asynchronous streaming of the media
        /// </summary>
        protected override void OpenMediaAsync()
        {
            // Init
            Debug.WriteLine("[VideoStreamSource::OpenMediaAsync]");

            Dictionary<MediaSourceAttributesKeys, string> sourceAttributes =
                new Dictionary<MediaSourceAttributesKeys, string>();
            List<MediaStreamDescription> availableStreams =
                new List<MediaStreamDescription>();

            PrepareVideo();

            availableStreams.Add(videoDesc);

            // a zero timespan is an infinite video
            sourceAttributes[MediaSourceAttributesKeys.Duration] =
                TimeSpan.FromSeconds(0).Ticks.ToString(CultureInfo.InvariantCulture);

            sourceAttributes[MediaSourceAttributesKeys.CanSeek] = false.ToString();

            // tell Silverlight that we've prepared and opened our video
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
                // Uh oh, audio doesn't work yet
                throw new NotImplementedException();
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
            // TODO 
            throw new NotImplementedException();
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
            Debug.WriteLine("[VideoStreamSource::SeekAsync]");
            ReportSeekCompleted(seekToTime); 
        }
    }
}
