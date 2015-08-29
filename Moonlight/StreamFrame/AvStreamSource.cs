using SharpDX;
using SharpDX.XAudio2;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Core;

namespace Moonlight
{
    class AvStreamSource
    {
        #region Class Variables

        private BlockingCollection<byte[]> pendingSamples = new BlockingCollection<byte[]>(1);
        private DateTime videoStart = new DateTime(0);
        private SourceVoice sourceVoice;

        #endregion Class Variables

        public void SetSourceVoice(SourceVoice sourceVoice)
        {
            this.sourceVoice = sourceVoice;
        }

        public void Start()
        {
            sourceVoice.Start();
        }

        public void Stop()
        {
            // Wake up the sample requested thread
            pendingSamples.Add(null);

            // Wake up the enqueue thread
            pendingSamples.CompleteAdding();
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
            // Block until a sample is available from the queue
            byte[] sample = pendingSamples.Take();

            if (sample == null)
            {
                // This wakeup was for termination
                return;
            }

            // Return the sample
            args.Request.Sample = CreateVideoSample(sample);
        }

        public void EnqueueVideoSample(byte[] buf)
        {
            // This puts back-pressure in the DU queue in
            // common. It's needed so that we avoid our queue getting
            // too large.

            // On stop, the queue is "completed" so no more adds are accepted
            if (pendingSamples.IsAddingCompleted)
            {
                return;
            }

            // Try to queue the sample
            try
            {
                pendingSamples.Add(buf);
            }
            catch (InvalidOperationException)
            {
                // There's still a race where we can only see that adds are completed
                // when the add actually happens.
                return;
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
