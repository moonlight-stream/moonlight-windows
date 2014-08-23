using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace Limelight
{
    class AvStreamSource
    {
        #region Class Variables

        private MediaStreamSourceSampleRequest pendingVideoRequest;
        private MediaStreamSourceSampleRequest pendingAudioRequest;
        private Queue<MediaStreamSample> videoSampleQueue;
        private Queue<MediaStreamSample> audioSampleQueue;
        private object videoQueueLock;
        private object audioQueueLock;

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
            MediaStreamSample sample = MediaStreamSample.CreateFromBuffer(buf.AsBuffer(), TimeSpan.Zero);
            sample.Duration = TimeSpan.Zero;

            return sample;
        }

        private MediaStreamSample CreateAudioSample(byte[] buf)
        {
            MediaStreamSample sample = MediaStreamSample.CreateFromBuffer(buf.AsBuffer(), TimeSpan.Zero);
            sample.Duration = TimeSpan.Zero;

            return sample;
        }

        public void VideoSampleRequested(MediaStreamSourceSampleRequestedEventArgs args)
        {
            lock (videoQueueLock)
            {
                if (videoSampleQueue.Count > 0)
                {
                    // Satisfy the sample request with a queued sample
                    args.Request.Sample = videoSampleQueue.Dequeue();
                    Debug.WriteLine("Satisfying from queue");
                }
                else
                {
                    // This request is now pending
                    pendingVideoRequest = args.Request;
                }
            }
        }

        public void AudioSampleRequested(MediaStreamSourceSampleRequestedEventArgs args)
        {
            lock (audioQueueLock)
            {
                if (audioSampleQueue.Count > 0)
                {
                    Task.Run(() =>
                    {
                        // Satisfy the sample request with a queued sample
                        args.Request.Sample = audioSampleQueue.Dequeue();
                        args.Request.GetDeferral().Complete();
                    });
                }
                else
                {
                    // This request is now pending
                    pendingAudioRequest = args.Request;
                }
            }
        }

        public void EnqueueVideoSample(byte[] buf)
        {
            MediaStreamSample sample = CreateVideoSample(buf);

            lock (videoQueueLock)
            {
                if (pendingVideoRequest != null)
                {
                    MediaStreamSourceSampleRequest request = pendingVideoRequest;
                    pendingVideoRequest = null;

                    Task.Run(() =>
                    {
                        request.Sample = sample;
                        request.GetDeferral().Complete();
                    });
                }
                else
                {
                    videoSampleQueue.Enqueue(sample);
                }
            }
        }

        public void EnqueueAudioSample(byte[] buf)
        {
            MediaStreamSample sample = CreateAudioSample(buf);

            lock (audioQueueLock)
            {
                if (pendingAudioRequest != null)
                {
                    pendingAudioRequest.Sample = sample;
                    pendingAudioRequest.GetDeferral().Complete();

                    pendingAudioRequest = null;
                }
                else
                {
                    audioSampleQueue.Enqueue(sample);
                }
            }
        }
    }
}
