using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Limelight
{
    public enum Resolutions { WVGA, WXGA, HD };
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();


            //for when we dynamically tell what resolution the phone is
            /*
            List<String> ResolutionAndFrameRateSource = new List<String>();
            switch(ResolutionHelper.CurrentResolution)
            {
                case Resolutions.HD:
                    ResolutionAndFrameRateSource.Add("720p (30FPS)");
                    ResolutionAndFrameRateSource.Add("720p (60FPS)");
                    break;
                default:
                    ResolutionAndFrameRateSource.Add("YOU BROKE SOMETHING");
                    ResolutionAndFrameRateSource.Add("Or no HD modes are avaialble.");
                    break;
            }
            */
        }
    }

    

    public static class ResolutionHelper
    {
        private static bool IsWvga
        {
            get
            {
                return App.Current.Host.Content.ScaleFactor == 100;
            }
        }

        private static bool IsWxga
        {
            get
            {
                return App.Current.Host.Content.ScaleFactor == 160;
            }
        }

        private static bool IsHD
        {
            get
            {
                return App.Current.Host.Content.ScaleFactor == 150;
            }
        }

        public static Resolutions CurrentResolution
        {
            get
            {
                if (IsWvga) return Resolutions.WVGA;
                else if (IsWxga) return Resolutions.WXGA;
                else if (IsHD) return Resolutions.HD;
                else throw new InvalidOperationException("Unknown resolution");
            }
        }
    }
}