using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Navigation;

namespace Limelight
{
    /// <summary>
    /// Main class for limelight-windows-phone
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        BackgroundWorker bw = new BackgroundWorker();
        int steamId = 0; 

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
            NvHttp nv = new NvHttp(host_textbox.Text);
            XmlQuery pairState = new XmlQuery(nv.baseUrl + "/pairstate?uniqueid=" + nv.GetDeviceName());
            if (pairState.GetErrorMessage() != null)
            {
                MessageBox.Show("Failed to get pair state");
            }
            else if (String.Compare(pairState.XmlAttribute("paired"), "0") == 0)
            {
                MessageBox.Show("Device not paired");
            }
            else if (steamId == 0)
            {
                MessageBox.Show("Failed to find Steam");
            } 
            else
            {
                // Save the user's host input and send it to the streamframe page
                PhoneApplicationService.Current.State["host"] = host_textbox.Text;
                NavigationService.Navigate(new Uri("/StreamFrame.xaml?steamId="+steamId, UriKind.Relative));
            }

        }

        /// <summary>
        /// Executed when the user presses "Pair"
        /// </summary>
        private void PairButton_Click(object sender, RoutedEventArgs e)
        {
            bw.RunWorkerAsync(host_textbox.Text);
            SaveSettings(); 
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
            Debug.WriteLine("Pairing ");
            // Create NvHttp object with the user input as the URL
            NvHttp nv = new NvHttp((string)e.Argument);
            XmlQuery pairInfo = new XmlQuery(nv.baseUrl + "/pair?uniqueid=" + nv.GetDeviceName());
            if (pairInfo.GetErrorMessage() != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Pairing failed: " + pairInfo.GetErrorMessage())));
                bw.CancelAsync();
            }
            // Session ID = 0; pairing failed
            else if (String.Compare(pairInfo.XmlAttribute("sessionid"), "0") == 0 )
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Pairing failed: Session ID = 0")));
                bw.CancelAsync();
            }
                // Session ID is okay - try to get the app list
            else
            {
                XmlQuery appList = new XmlQuery(nv.baseUrl + "/applist?uniqueid=" + nv.GetDeviceName());
                // Error querying app list
                if (appList.GetErrorMessage() != null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("App list query failed: " + appList.GetErrorMessage())));
                    bw.CancelAsync();
                }
                    // App list query went well - try to get the steam ID
                else
                {
                    string steamIdStr = appList.XmlAttributeFromElement("ID", appList.XmlAttributeElement("App"));
                    // Steam ID failed
                    if (appList.GetErrorMessage() != null)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Failed to get Steam ID: " + appList.GetErrorMessage())));
                    }
                        // We're in the clear - save the Steam app ID and tell the user all went well
                    else
                    {
                        steamId = Convert.ToInt32(steamIdStr);
                        Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Pairing successfully completed")));
                    }
                }
            }
        }
       
        // <summary>
        /// Runs once the background worker completes
        /// </summary>
        void bwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Check to see if an error occurred in the background process.
            if (e.Error != null)
            {
                MessageBox.Show("Pairing error: " + e.Error.Message);
            }

            // If the connection attempt was manually cancelled
            else if (e.Cancelled)
            {
                MessageBox.Show("Pairing error");
            }

            // Everything completed normally
            else
            {
                Debug.WriteLine("Pairing background Worker Successfully Completed");
            }
        }

        #endregion Background Worker
    }
}