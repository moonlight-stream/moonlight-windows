using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage.Streams;

namespace Limelight
{
    // FIXME use 8.1/RT version of this
    class AvStreamSource : IDisposable , IMediaSource
    {
        #region Audio/Video Sample Constructors
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
        #endregion Audio/Video Sample Constructors

        #region Class Variables

        private int frameWidth;
        private int frameHeight;
        private ManualResetEvent shutdownEvent;
        //private Dictionary<MediaSampleAttributeKeys, string> emptySampleDict = new Dictionary<MediaSampleAttributeKeys, string>();
        private ulong frameNumber;

        private volatile int outstandingGetVideoSampleCount;
        private volatile int outstandingGetAudioSampleCount;
        private Queue<VideoSample> nalQueue;
        private Queue<AudioSample> audioQueue;
        private object nalQueueLock = new object();
        private object audioQueueLock = new object();

        #endregion Class Variables

        #region Constructor
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
            this.audioQueue = new Queue<AudioSample>();

            // 15 is the minimum size
            //this.AudioBufferLength = 15;
        }
        #endregion Constructor

        #region IDisposible Implementation
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
        #endregion IDisposible Implementation
    }
}
