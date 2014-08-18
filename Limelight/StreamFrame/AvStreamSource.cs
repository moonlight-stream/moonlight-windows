using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Core;

namespace Limelight
{
    class AvStreamSource
    {
        #region Class Variables

        private MediaStreamSourceSampleRequestDeferral pendingVideoDeferral;
        private MediaStreamSourceSampleRequest pendingVideoRequest;
        private MediaStreamSourceSampleRequestDeferral pendingAudioDeferral;
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

        private MediaStreamSample CreateVideoSample(byte[] buf, int nalStart, int nalEnd)
        {
            int nalLength = nalEnd - nalStart;
            byte[] nal = new byte[nalLength];
            Array.ConstrainedCopy(buf, nalStart, nal, 0, nalLength);

            MediaStreamSample sample = MediaStreamSample.CreateFromBuffer(nal.AsBuffer(), TimeSpan.Zero);
            sample.Duration = TimeSpan.Zero;

            return sample;
        }

        private MediaStreamSample CreateAudioSample(byte[] buf)
        {
            MediaStreamSample sample = MediaStreamSample.CreateFromBuffer(buf.AsBuffer(), TimeSpan.Zero);
            sample.Duration = TimeSpan.Zero;

            return sample;
        }

        private void EnqueueNal(byte[] buf, int nalStart, int nalEnd)
        {
            MediaStreamSample sample = CreateVideoSample(buf, nalStart, nalEnd);

            lock (videoQueueLock)
            {
                if (pendingVideoRequest != null)
                {
                    pendingVideoRequest.Sample = sample;
                    pendingVideoDeferral.Complete();

                    pendingVideoRequest = null;
                    pendingVideoDeferral = null;
                }
            }
        }

        public void VideoSampleRequested(MediaStreamSourceSampleRequestedEventArgs args)
        {
            lock (videoQueueLock)
            {
                if (videoSampleQueue.Count > 0)
                {
                    // Satisfy the sample request with a queued sample
                    args.Request.Sample = videoSampleQueue.Dequeue();
                }
                else
                {
                    // This request is now pending
                    pendingVideoRequest = args.Request;
                    pendingVideoDeferral = args.Request.GetDeferral();
                }
            }
        }

        public void AudioSampleRequested(MediaStreamSourceSampleRequestedEventArgs args)
        {
            lock (audioQueueLock)
            {
                if (audioSampleQueue.Count > 0)
                {
                    // Satisfy the sample request with a queued sample
                    args.Request.Sample = audioSampleQueue.Dequeue();
                }
                else
                {
                    // This request is now pending
                    pendingAudioRequest = args.Request;
                    pendingAudioDeferral = args.Request.GetDeferral();
                }
            }
        }

        public void EnqueueVideoSample(byte[] buf)
        {
            /*int i;

            int currentNalStart = -1;
            bool frameStart = false;
            for (i = 0; i < buf.Length - 4; i++)
            {
                // Look for the Annex B NAL start sequence (0x000001)
                if (buf[i] == 0 && buf[i + 1] == 0)
                {
                    // Check for frame start
                    if (buf[i + 2] == 0 && buf[i + 3] == 1)
                    {
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
                        EnqueueNal(buf, currentNalStart, i);
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
                EnqueueNal(buf, currentNalStart, buf.Length);
            }*/

            EnqueueNal(buf, 0, buf.Length);
        }

        public void EnqueueAudioSample(byte[] buf)
        {
            MediaStreamSample sample = CreateAudioSample(buf);

            lock (audioQueueLock)
            {
                if (pendingAudioRequest != null)
                {
                    pendingAudioRequest.Sample = sample;
                    pendingAudioDeferral.Complete();

                    pendingAudioRequest = null;
                    pendingAudioDeferral = null;
                }
            }
        }
    }
}
