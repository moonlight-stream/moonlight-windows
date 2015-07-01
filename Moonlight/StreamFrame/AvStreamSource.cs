using SharpDX;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Moonlight
{
    class AvStreamSource
    {
        #region Class Variables

        private MediaStreamSourceSampleRequest pendingVideoRequest;
        private MediaStreamSourceSampleRequestDeferral pendingVideoDeferral;
        private MediaStreamSample pendingVideoSample;
        private ManualResetEvent removedVideoSampleEvent = new ManualResetEvent(false);
        private object videoQueueLock;
        private DateTime videoStart = new DateTime(0);
        private SourceVoice sourceVoice;

        #endregion Class Variables

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="AvStreamSource"/> class. 
        /// </summary>
        public AvStreamSource()
        {
            this.videoQueueLock = new object();
        }
        #endregion Constructor

        public void SetSourceVoice(SourceVoice sourceVoice)
        {
            this.sourceVoice = sourceVoice;
            sourceVoice.Start();
        }

        private MediaStreamSample CreateVideoSample(byte[] buf)
        {
            if (videoStart.Ticks == 0)
            {
                videoStart = DateTime.Now;
            }

            // Marshal this buffer so we can safely queue it without worrying about
            // reuse of the memory backing it
            byte[] bufCopy = new byte[buf.Length];
            Array.Copy(buf, bufCopy, buf.Length);

            MediaStreamSample sample = MediaStreamSample.CreateFromBuffer(bufCopy.AsBuffer(),
                DateTime.Now - videoStart);
            sample.Duration = TimeSpan.Zero;

            if ((buf[4] & 0x1F) == 0x5)
            {
                sample.KeyFrame = true;
            }

            return sample;
        }

        public void VideoSampleRequested(MediaStreamSourceSampleRequestedEventArgs args)
        {
            lock (videoQueueLock)
            {
                if (pendingVideoSample != null)
                {
                    args.Request.Sample = pendingVideoSample;
                    pendingVideoSample = null;
                }
                else
                {
                    pendingVideoRequest = args.Request;
                    pendingVideoDeferral = args.Request.GetDeferral();
                }
            }

            removedVideoSampleEvent.Set();
        }

        public void EnqueueVideoSample(byte[] buf)
        {
            // This puts back-pressure in the DU queue in
            // common. It's needed so that we avoid our queue getting
            // too large.

            // Try to enqueue if there's space
            lock (videoQueueLock)
            {
                // Attempt to satisfy a deferral first
                if (pendingVideoRequest != null)
                {
                    pendingVideoRequest.Sample = CreateVideoSample(buf);
                    pendingVideoDeferral.Complete();

                    pendingVideoRequest = null;
                    pendingVideoDeferral = null;
                    return;
                }

                // Now pend the sample if nothing is already waiting
                if (pendingVideoSample == null)
                {
                    pendingVideoSample = CreateVideoSample(buf);
                    return;
                }
            }

            // Otherwise wait until there's space
            for (;;)
            {
                removedVideoSampleEvent.WaitOne();

                lock (videoQueueLock)
                {
                    if (pendingVideoSample == null)
                    {
                        pendingVideoSample = CreateVideoSample(buf);
                        return;
                    }
                }
            }
        }

        public void EnqueueAudioSample(byte[] buf)
        {
            // Allocate a new buffer because ours will go away after this call
            AudioBuffer buffer = new AudioBuffer(DataStream.Create<byte>(buf, true, false, 0, false));

            try
            {
                sourceVoice.SubmitSourceBuffer(buffer, null);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Audio exception");
                Debug.WriteLine(e.Message);
            }
        }
    }
}
