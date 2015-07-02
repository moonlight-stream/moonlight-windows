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

        private MediaStreamSample pendingVideoSample;
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

            // HACK: Marking all frames as keyframes seems
            // to keep the decoder from dying after the first
            // few seconds.
            sample.KeyFrame = true;

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
                    return;
                }
            }

            // If we don't have any sample right now, we just return an empty sample. This tells the decoder
            // that we're still alive here. Doing a sample deferral seems to cause serious lag issues.
            args.Request.Sample = MediaStreamSample.CreateFromBuffer(new byte[0].AsBuffer(), TimeSpan.Zero);
        }

        public void EnqueueVideoSample(byte[] buf)
        {
            // This puts back-pressure in the DU queue in
            // common. It's needed so that we avoid our queue getting
            // too large.

            MediaStreamSample sample = CreateVideoSample(buf);

            // Wait until there's space to queue
            for (;;)
            {
                lock (videoQueueLock)
                {
                    if (pendingVideoSample == null)
                    {
                        pendingVideoSample = sample;
                        return;
                    }
                }
            }
        }

        public void EnqueueAudioSample(byte[] buf)
        {
            // Allocate a new buffer because ours will go away after this call
            AudioBuffer buffer = new AudioBuffer(DataStream.Create<byte>(buf, true, false));

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
