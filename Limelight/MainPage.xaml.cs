using Limelight.Resources;
using Limelight_common_binding;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Limelight
{
    /// <summary>
    /// Main class for limelight-windows-phone
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        static String hostAddr; 

        /// <summary>
        /// Initializes a new instance of the MainPage class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the host address
        /// </summary>
        /// <returns>The host address</returns>
        public String getHostAddr()
        {
            return hostAddr; 
        }
        
        /// <summary>
        /// Executed when the user presses "Start Streaming Steam!"
        /// </summary>
        private void StreamButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Start Streaming button pressed");
            LimelightStreamConfiguration streamConfig = new LimelightStreamConfiguration(1280, 720, 30);
            Debug.WriteLine("Starting connection\n");
            LimelightCommonRuntimeComponent.StartConnection((uint)IPAddress.HostToNetworkOrder((int)IPAddress.Parse("129.22.46.110").Address), streamConfig);
            NavigationService.Navigate(new Uri("/StreamFrame.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Executed when the user presses "Pair"
        /// </summary>
        private void PairButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Pair button pressed");
            hostAddr = host_textbox.Text; 

            // TODO call currently non-existent pair method
        }
    }
}