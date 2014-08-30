using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Limelight
{
    class AvStreamSource
    {
        #region Class Variables

        private MediaStreamSourceSampleRequest pendingVideoRequest;
        private MediaStreamSourceSampleRequestDeferral pendingVideoDeferral;
        private MediaStreamSourceSampleRequest pendingAudioRequest;
        private MediaStreamSourceSampleRequestDeferral pendingAudioDeferral;
        private Queue<MediaStreamSample> videoSampleQueue;
        private Queue<MediaStreamSample> audioSampleQueue;
        private object videoQueueLock;
        private object audioQueueLock;
        private DateTime videoStart = new DateTime(0);
        private DateTime audioStart = new DateTime(0);

        #endregion Class Variables

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="AvStreamSource"/> class. 
        /// </summary>
        public AvStreamSource()
        {
            this.videoSampleQueue = new Queue<MediaStreamSample>();
            this.audioSampleQueue = new Queue<MediaStreamSample>();
            this.videoQueueLock = new object();
            this.audioQueueLock = new object();
        }
        #endregion Constructor


        private MediaStreamSample CreateVideoSample(byte[] buf)
        {
            if (videoStart.Ticks == 0)
            {
                videoStart = DateTime.Now;
            }

            MediaStreamSample sample = MediaStreamSample.CreateFromBuffer(buf.AsBuffer(),
                DateTime.Now - videoStart);
            sample.DecodeTimestamp = sample.Timestamp;
            sample.Duration = TimeSpan.Zero;

            switch (buf[4])
            {
                case 0x65:
                    sample.KeyFrame = true;
                    //Debug.WriteLine("I-frame");
                    break;

                case 0x67:
                    //Debug.WriteLine("SPS");
                    break;

                case 0x68:
                    //Debug.WriteLine("PPS");
                    break;

                case 0x61:
                    break;

                default:
                    Debug.WriteLine("Unrecognized data: "+buf[4].ToString());
                    break;
            }

            return sample;
        }

        private MediaStreamSample CreateAudioSample(byte[] buf)
        {
            if (audioStart.Ticks == 0)
            {
                audioStart = DateTime.Now;
            }

            MediaStreamSample sample = MediaStreamSample.CreateFromBuffer(buf.AsBuffer(),
                DateTime.Now - audioStart);
            sample.Duration = TimeSpan.FromMilliseconds(5);

            return sample;
        }

        public void VideoSampleRequested(MediaStreamSourceSampleRequestedEventArgs args)
        {
            lock (videoQueueLock)
            {
                pendingVideoRequest = args.Request;
                pendingVideoDeferral = args.Request.GetDeferral();
            }
        }

        public void AudioSampleRequested(MediaStreamSourceSampleRequestedEventArgs args)
        {
            lock (audioQueueLock)
            {
                pendingAudioRequest = args.Request;
                pendingAudioDeferral = args.Request.GetDeferral();
            }
        }

        public void EnqueueVideoSample(byte[] buf)
        {
            MediaStreamSample sample = CreateVideoSample(buf);

            // This loop puts back-pressure in the DU queue in
            // common. It's needed so that we avoid our queue getting
            // too large.
            for (;;)
            {
                lock (videoQueueLock)
                {
                    if (pendingVideoRequest == null)
                    {
                        continue;
                    }

                    pendingVideoRequest.Sample = sample;
                    pendingVideoDeferral.Complete();

                    pendingVideoRequest = null;
                    break;
                }
            }
        }

        public void EnqueueAudioSample(byte[] buf)
        {
            MediaStreamSample sample = CreateAudioSample(buf);

            // This loop puts back-pressure in the DU queue in
            // common. It's needed so that we avoid our queue getting
            // too large.
            for (; ; )
            {
                lock (audioQueueLock)
                {
                    if (pendingAudioRequest == null)
                    {
                        continue;
                    }

                    pendingAudioRequest.Sample = sample;
                    pendingAudioDeferral.Complete();

                    pendingAudioRequest = null;
                    break;
                }
            }
        }
    }
}
