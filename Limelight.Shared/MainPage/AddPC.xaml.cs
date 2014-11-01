using Limelight.Streaming;
using Limelight.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Limelight
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddPC : Page
    {
        public AddPC()
        {
            this.InitializeComponent();

#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtonsBackPressed;
#endif
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// If Windows Phone, go backwards instead of quitting the app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HardwareButtonsBackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            e.Handled = true;
            Frame.GoBack();
        }
#endif

        /// <summary>
        /// 
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

    }
}
