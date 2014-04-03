namespace Limelight
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows.Media;

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
            /// <param name="presentationTime">The time at which a sample should be rendered as measured in 100 nanosecond increments</param>
            /// <param name="sampleDuration">The duration of the sample</param>
            public VideoSample(Windows.Storage.Streams.IBuffer buffer, ulong presentationTime, ulong sampleDuration)
            {
                this.buffer = buffer;
                this.presentationTime = presentationTime;
                this.sampleDuration = sampleDuration; 
            }
            internal Windows.Storage.Streams.IBuffer buffer { get; private set; }
            internal ulong presentationTime { get; private set; }
            internal ulong sampleDuration { get; private set; }
        }

        private int frameWidth;
        private int frameHeight;
        private Queue<VideoSample> sampleQueue;
        private object lockObj = new object();
        private ManualResetEvent shutdownEvent;
        private volatile int outstandingGetVideoSampleCount;
        private MediaStreamDescription videoDesc;
        private Dictionary<MediaSampleAttributeKeys, string> emptySampleDict = new Dictionary<MediaSampleAttributeKeys, string>();

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
            this.sampleQueue = new Queue<VideoSample>();
            this.outstandingGetVideoSampleCount = 0;
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

        /// <summary>
        /// Enqueues next video samples in the buffer
        /// </summary>
        /// <param name="buf">Buffer for the video stream</param>
        /// <param name="presentationTime">The time at which a sample should be rendered as measured in 100 nanosecond increments</param>
        /// <param name="sampleDuration">The duration of the sample</param>
        public void EnqueueSamples(Windows.Storage.Streams.IBuffer buf, ulong presentationTime, ulong sampleDuration)
        {
            lock (lockObj)
            {
                sampleQueue.Enqueue(new VideoSample(buf, presentationTime, sampleDuration));
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
                    if (sampleQueue.Count() == 0)
                    {
                        return;
                    }
                    videoSample = sampleQueue.Dequeue();
                }

                Stream sampleStream = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsStream(videoSample.buffer);

                // Send out the next sample
                MediaStreamSample mediaStreamSamp = new MediaStreamSample(
                    videoDesc,
                    sampleStream,
                    0,
                    sampleStream.Length,
                    (long)videoSample.presentationTime,
                    (long)videoSample.sampleDuration,
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
