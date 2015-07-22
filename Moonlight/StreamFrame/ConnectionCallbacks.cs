namespace Moonlight
{
    using Moonlight.Utils;
    using Moonlight_common_binding;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.System.Threading;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    public sealed partial class StreamFrame : Page
    {
        Moonlight.Controllers.XInput controllers;

        #region Decoder Renderer
        public void DrSetup(int width, int height, int redrawRate, int drFlags)
        {

        }

        public void DrCleanup()
        {

        }

        public int DrSubmitDecodeUnit(byte[] data)
        {
            AvStream.EnqueueVideoSample(data);
            return 0;
        }
#endregion Decoder Renderer

        #region Audio Renderer
        public void ArInit()
        {

        }

        public void ArCleanup()
        {

        }

        public void ArPlaySample(byte[] data)
        {
            AvStream.EnqueueAudioSample(data);
        }
#endregion Audio Renderer

        #region Connection Listener
        /// <summary>
        /// Stage beginning callback. Updates the connection progress bar with the current stage
        /// </summary>
        /// <param name="stage"></param>
        public async void ClStageStarting(int stage)
        {
            String stateText = "";
            switch (stage)
            {
                case STAGE_PLATFORM_INIT:
                    stateText = "Initializing platform...";
                    break;
                case STAGE_HANDSHAKE:
                    stateText = "Starting handshake...";
                    break;
                case STAGE_CONTROL_STREAM_INIT:
                    stateText = "Initializing control stream...";
                    break;
                case STAGE_VIDEO_STREAM_INIT:
                    stateText = "Initializing video stream...";
                    break;
                case STAGE_AUDIO_STREAM_INIT:
                    stateText = "Initializing audio stream...";
                    break;
                case STAGE_INPUT_STREAM_INIT:
                    stateText = "Initializing input stream...";
                    break;
                case STAGE_CONTROL_STREAM_START:
                    stateText = "Starting control stream...";
                    break;
                case STAGE_VIDEO_STREAM_START:
                    stateText = "Starting video stream...";
                    break;
                case STAGE_AUDIO_STREAM_START:
                    stateText = "Starting audio stream...";
                    break;
                case STAGE_INPUT_STREAM_START:
                    stateText = "Starting input stream...";
                    break;
            }
            await SetStateText(stateText);
        }

        /// <summary>
        /// Connection stage complete callback
        /// </summary>
        /// <param name="stage">Stage number</param>
        public void ClStageComplete(int stage)
        {

        }

        /// <summary>
        /// Connection stage failed callback
        /// </summary>
        /// <param name="stage">Stage number</param>
        /// <param name="errorCode">Error code for stage failure</param>
        public void ClStageFailed(int stage, int errorCode)
        {
            switch (stage)
            {
                case STAGE_PLATFORM_INIT:
                    stageFailureText = "Initializing platform failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_HANDSHAKE:
                    stageFailureText = "Starting handshake failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_CONTROL_STREAM_INIT:
                    stageFailureText = "Initializing control stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_VIDEO_STREAM_INIT:
                    stageFailureText = "Initializing video stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_AUDIO_STREAM_INIT:
                    stageFailureText = "Initializing audio stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_INPUT_STREAM_INIT:
                    stageFailureText = "Initializing input stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_CONTROL_STREAM_START:
                    stageFailureText = "Starting control stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_VIDEO_STREAM_START:
                    stageFailureText = "Starting video stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_AUDIO_STREAM_START:
                    stageFailureText = "Starting audio stream failed. Error: " + errorCode.ToString();
                    break;
                case STAGE_INPUT_STREAM_START:
                    stageFailureText = "Starting input stream failed. Error: " + errorCode.ToString();
                    break;
            }
        }

        /// <summary>
        /// Connection stage started callback
        /// </summary>
        public void ClConnectionStarted()
        {
            // Capture the mouse
            CaptureMouse();

            // Start controller support code
            controllers = new Moonlight.Controllers.XInput();
            controllers.Start();
        }

        /// <summary>
        /// Connection terminated callback
        /// </summary>
        /// <param name="errorCode">Error code for connection terminated</param>
        public void ClConnectionTerminated(int errorCode)
        {
            Debug.WriteLine("Connection terminated: " + errorCode);

            if (controllers != null)
            {
                // Stop controller code
                controllers.Stop();
            }

            var unused = Task.Run(() =>
            {
                // This needs to be done on a separate thread
                MoonlightCommonRuntimeComponent.StopConnection();
            });

            DialogUtils.DisplayDialog(this.Dispatcher, "Connection terminated unexpectedly", "Connection Terminated", (command) =>
            {
                this.Frame.Navigate(typeof(MainPage), null);
            });
        }

        public void ClDisplayMessage(String message)
        {
            Debug.WriteLine("ClDisplayMessage: " + message);
        }

        public void ClDisplayTransientMessage(String message)
        {
            Debug.WriteLine("ClDisplayTransientMessage: " + message);
        }
#endregion Connection Listener
    }
}