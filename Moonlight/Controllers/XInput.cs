using Moonlight_common_binding;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Moonlight.Controllers
{
    public class XInput
    {
        private Controller[] controllers;
        private bool stopPolling = false;

        private async Task PollControllerWorker()
        {
            int lastPacketNumber = -1;

            while (!stopPolling)
            {
                // Wait before collecting new data
                await Task.Delay(25);

                // Read input from all controllers
                foreach (Controller controller in controllers)
                {
                    // Don't do anything if no controller is connected
                    if (controller.IsConnected == false)
                    {
                        if (lastPacketNumber != -1)
                        {
                            // Set all inputs back to zero
                            MoonlightCommonRuntimeComponent.SendControllerInput((short)controller.UserIndex, 0, 0, 0, 0, 0, 0);
                            lastPacketNumber = -1;
                        }

                        // Not connected
                        continue;
                    }

                    // Snapshot the state
                    State state = controller.GetState();
                    if (state.PacketNumber != lastPacketNumber)
                    {
                        int buttonFlags = 0;

                        // Convert XInput constants to our button flags
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.Up;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.Down;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.Left;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.DPadRight) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.Right;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.A) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.A;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.B) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.B;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.X) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.X;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.Y) != 0)
                        {
                            buttonFlags |= (int)ButtonFlags.Y;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.LB;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.RB;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.Back) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.Back;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.Start) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.Play;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.LeftThumb) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.LS;
                        }
                        if ((state.Gamepad.Buttons & GamepadButtonFlags.RightThumb) != 0)
                        {
                            buttonFlags |= (short)ButtonFlags.RS;
                        }

                        // Send the controller input packet
                        MoonlightCommonRuntimeComponent.SendMultiControllerInput(
                            (short)controller.UserIndex,
                            (short)buttonFlags,
                            state.Gamepad.LeftTrigger,
                            state.Gamepad.RightTrigger,
                            state.Gamepad.LeftThumbX,
                            state.Gamepad.LeftThumbY,
                            state.Gamepad.RightThumbX,
                            state.Gamepad.RightThumbY);
                    }
                }
            }
        }

        public void Start()
        {
            controllers = new Controller[] {
                new Controller(UserIndex.One),
                new Controller(UserIndex.Two),
                new Controller(UserIndex.Three),
                new Controller(UserIndex.Four),
            };

            Task.Run(PollControllerWorker);
        }

        public void Stop()
        {
            stopPolling = true;
        }
    }
}