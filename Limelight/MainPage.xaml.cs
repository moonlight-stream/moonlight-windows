using Limelight.Resources;
using Limelight_common_binding;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Limelight
{
    /// <summary>
    /// Main class for limelight-windows-phone
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {

        BackgroundWorker bw = new BackgroundWorker(); 
        /// <summary>
        /// Initializes a new instance of the MainPage class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            LoadSettings(); 

            // Set up background worker for pairing
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bwDoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwRunWorkerCompleted);
        }

        #region Event Handlers
        /// <summary>
        /// Executed when the user presses "Start Streaming Steam!"
        /// </summary>
        private void StreamButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings(); 
            Debug.WriteLine("Start Streaming button pressed");

            // Save the user's host input and send it to the streamframe page
            PhoneApplicationService.Current.State["host"] = host_textbox.Text; 
            NavigationService.Navigate(new Uri("/StreamFrame.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Executed when the user presses "Pair"
        /// </summary>
        private void PairButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings(); 
            bw.RunWorkerAsync(); 
        }

        /// <summary>
        /// Save the settings the user has set so they can be persisted
        /// </summary>
        private void SaveSettings()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            // Save hostname text box
            if (!settings.Contains("hostname"))
            {
                settings.Add("hostname", host_textbox.Text);
            }
            else
            {
                settings["hostname"] = host_textbox.Text;
            }
            settings.Save();
        }

        /// <summary>
        /// Load the user settings
        /// </summary>
        private void LoadSettings()
        {
            // Load hostname
            if (IsolatedStorageSettings.ApplicationSettings.Contains("hostname"))
            {
                host_textbox.Text = IsolatedStorageSettings.ApplicationSettings["hostname"] as string;
            }
        }

        #endregion Event Handlers

        #region Background Worker

        /// <summary>
        /// Event handler for Background Worker's doWork event.
        /// </summary>
        private void bwDoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("Doing work");
            // TODO do the pair thing
            NvHttp.GetMacAddressString(); 
        }

        // <summary>
        /// Runs once the background worker completes
        /// </summary>
        void bwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Check to see if an error occurred in the background process.
            if (e.Error != null)
            {
                Debug.WriteLine("Error while performing pairing.");
            }

            // If the connection attempt was cancelled by a failed stage
            else if (e.Cancelled)
            {
                Debug.WriteLine("Pairing cancelled.");
            }

            // Everything completed normally - bring the user to the stream frame
            else
            {
                Debug.WriteLine("Pairing background Worker Successfully Completed");
            }
        }

        #endregion Background Worker
    }
}