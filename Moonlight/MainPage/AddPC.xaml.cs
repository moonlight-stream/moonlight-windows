using Moonlight.Streaming;
using Moonlight.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Moonlight
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddPC : Page
    {
        public AddPC()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Event handler for clicking the add PC manually button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(ip_textbox.Text) || String.IsNullOrWhiteSpace(nickname_textbox.Text))
            {
                DialogUtils.DisplayDialog(this.Dispatcher, "Please fill out both text boxes", "Add PC Failed");
                return;
            }
            else
            {
                Computer toAdd = new Computer(nickname_textbox.Text, ip_textbox.Text);                
                this.Frame.Navigate(typeof(MainPage), toAdd);
            }
        }

        /// <summary>
        /// Take the user back
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Back_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack(); 
        }
    }
}
