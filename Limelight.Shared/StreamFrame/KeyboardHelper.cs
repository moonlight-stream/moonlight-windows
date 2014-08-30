using Limelight_common_binding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.System;
using Windows.UI.Core;

namespace Limelight
{
    class KeyboardHelper
    {
        private const short KEY_PREFIX = (short) 0x80;
	    private const int VK_0 = 48;
	    private const int VK_9 = 57;
	    private const int VK_A = 65;
	    private const int VK_Z = 90;
	    private const int VK_ALT = 18;
	    private const int VK_NUMPAD0 = 96;
	    private const int VK_BACK_SLASH = 92;
	    private const int VK_CAPS_LOCK = 20;
	    private const int VK_CLEAR = 12;
	    private const int VK_COMMA = 44;
	    private const int VK_CONTROL = 17;
	    private const int VK_BACK_SPACE = 8;
	    private const int VK_EQUALS = 61;
	    private const int VK_ESCAPE = 27;
	    private const int VK_F1 = 112;
	    private const int VK_PERIOD = 46;
	    private const int VK_INSERT = 155;
	    private const int VK_OPEN_BRACKET = 91;
	    private const int VK_WINDOWS = 524;
	    private const int VK_MINUS = 45;
	    private const int VK_END = 35;
	    private const int VK_HOME = 36;
	    private const int VK_NUM_LOCK = 144;
	    private const int VK_PAGE_UP = 33;
	    private const int VK_PAGE_DOWN = 34;
	    private const int VK_PLUS = 521;
	    private const int VK_CLOSE_BRACKET = 93;
	    private const int VK_SCROLL_LOCK = 145;
	    private const int VK_SEMICOLON = 59;
	    private const int VK_SHIFT = 16;
	    private const int VK_SLASH = 47;
	    private const int VK_SPACE = 32;
	    private const int VK_PRINTSCREEN = 154;
	    private const int VK_TAB = 9;
	    private const int VK_LEFT = 37;
	    private const int VK_RIGHT = 39;
	    private const int VK_UP = 38;
	    private const int VK_DOWN = 40;
	    private const int VK_BACK_QUOTE = 192;
	    private const int VK_QUOTE = 222;
	    private const int VK_PAUSE = 19;

        public static byte GetModifierFlags()
        {
            byte flags;
            CoreWindow win = CoreWindow.GetForCurrentThread();

            flags = 0;

            if ((win.GetKeyState(VirtualKey.Shift) & CoreVirtualKeyStates.Down) != 0)
            {
                flags |= (byte)Modifier.ModifierShift;
            }

            if ((win.GetKeyState(VirtualKey.Menu) & CoreVirtualKeyStates.Down) != 0)
            {
                flags |= (byte)Modifier.ModifierAlt;
            }

            if ((win.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) != 0)
            {
                flags |= (byte)Modifier.ModifierCtrl;
            }

            return flags;
        }

        public static short TranslateVirtualKey(VirtualKey key)
        {
            int translated;

            if (key >= VirtualKey.Number0 &&
                key <= VirtualKey.Number9)
            {
                translated = (key - VirtualKey.Number0) + VK_0;
            }
            else if (key >= VirtualKey.A &&
                key <= VirtualKey.Z)
            {
                translated = (key - VirtualKey.A) + VK_A;
            }
            else if (key >= VirtualKey.NumberPad0 &&
                key <= VirtualKey.NumberPad9)
            {
                translated = (key - VirtualKey.NumberPad0) + VK_NUMPAD0;
            }
            else if (key >= VirtualKey.F1 &&
                key <= VirtualKey.F12)
            {
                translated = (key - VirtualKey.F1) + VK_F1;
            }
            else
            {
                switch (key)
                {
                    case VirtualKey.LeftMenu:
                    case VirtualKey.RightMenu:
                    case VirtualKey.Menu:
                        translated = VK_ALT;
                        break;
                    
                    //case VirtualKey.Backslash:

                    case VirtualKey.CapitalLock:
                        translated = VK_CAPS_LOCK;
                        break;

                    case VirtualKey.Clear:
                        translated = VK_CLEAR;
                        break;

                   //case VirtualKey.Comma

                    case VirtualKey.Control:
                    case VirtualKey.LeftControl:
                    case VirtualKey.RightControl:
                        translated = VK_CONTROL;
                        break;


                    //case VirtualKey.Backspace:

                    case VirtualKey.Enter:
                        translated = 0x0d;
                        break;

                    //case VirtualKey.Equals:

                    case VirtualKey.Escape:
                        translated = VK_ESCAPE;
                        break;

                    case VirtualKey.Delete:
                        // Nvidia maps period to delete
                        translated = VK_PERIOD;
                        break;

                    case VirtualKey.Insert:
                        translated = -1;
                        break;

                    //case VirtualKey.LeftBracket:

                    case VirtualKey.LeftWindows:
                    case VirtualKey.RightWindows:
                        translated = VK_WINDOWS;
                        break;

                    //case VirtualKey.Minus

                    case VirtualKey.End:
                        translated = VK_END;
                        break;

                    case VirtualKey.Home:
                        translated = VK_HOME;
                        break;

                    case VirtualKey.NumberKeyLock:
                        translated = VK_NUM_LOCK;
                        break;

                    case VirtualKey.PageDown:
                        translated = VK_PAGE_DOWN;
                        break;

                    case VirtualKey.PageUp:
                        translated = VK_PAGE_UP;
                        break;

                    //case VirtualKey.Period:

                    //case VirtualKey.RightBracket

                    case VirtualKey.Scroll:
                        translated = VK_SCROLL_LOCK;
                        break;

                    //case VirtualKey.Semicolon

                    case VirtualKey.Shift:
                    case VirtualKey.LeftShift:
                    case VirtualKey.RightShift:
                        translated = VK_SHIFT;
                        break;

                    //case VirtualKey.Slash

                    case VirtualKey.Space:
                        translated = VK_SPACE;
                        break;

                    //case VirtualKey.SysReq:

                    case VirtualKey.Tab:
                        translated = VK_TAB;
                        break;

                    case VirtualKey.Left:
                        translated = VK_LEFT;
                        break;

                    case VirtualKey.Right:
                        translated = VK_RIGHT;
                        break;

                    case VirtualKey.Up:
                        translated = VK_UP;
                        break;

                    case VirtualKey.Down:
                        translated = VK_DOWN;
                        break;

                    //case VirtualKey.Grave

                    //case VirtualKey.Apostrophe:

                    //case VirtualKey.Break:

                    default:
                        Debug.WriteLine("No key for " + key);
                        return 0;
                }
            }

            return (short)((KEY_PREFIX << 8) | translated);
        }
    }
}
