using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Limelight.Resources;

namespace Limelight
{
    /// <summary>
    /// Main class for limelight-windows-phone
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

        }

        private void _720p_button_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void _30fps_button_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void StreamButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Start Streaming button pressed");
            NavigationService.Navigate(new Uri("/StreamFrame.xaml", UriKind.Relative));
        }

        private void PairButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Pair button pressed");
        }
    }
}