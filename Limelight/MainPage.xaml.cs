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
        #region Class variables

        private BackgroundWorker pairBw = new BackgroundWorker();
        private BackgroundWorker streamBw = new BackgroundWorker();
        private NvHttp nv;
        private int steamId = 0;
        #endregion Class variables

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainPage class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            LoadSettings(); 

            // Set up background worker for pairing
            pairBw.WorkerSupportsCancellation = true;
            pairBw.DoWork += new DoWorkEventHandler(pairBwDoWork);
            pairBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(pairBwRunWorkerCompleted);

            // Set up background worker for stream setup
            streamBw.WorkerSupportsCancellation = true;
            streamBw.DoWork += new DoWorkEventHandler(streamBwDoWork);
            streamBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(streamBwRunWorkerCompleted);
        }
        #endregion Constructor

        #region Event Handlers
        /// <summary>
        /// Executed when the user presses "Start Streaming Steam!"
        /// </summary>
        private void StreamButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            status_text.Text = "Checking pair state...";
            Debug.WriteLine("Start Streaming button pressed");
            streamBw.RunWorkerAsync(host_textbox.Text); 
        }

        /// <summary>
        /// Executed when the user presses "Pair"
        /// </summary>
        private void PairButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings(); 
            status_text.Text = "Pairing...";
            pairBw.RunWorkerAsync(host_textbox.Text);
        }
        #endregion Event Handlers  

        #region Background Workers
        /// <summary>
        /// When the user presses "Start Streaming Steam", first check that they are paired in the background worker
        /// </summary>
        private void streamBwDoWork(object sender, DoWorkEventArgs e)
        {
            nv = new NvHttp((string)e.Argument);
            // If device is already paired, don't cancel. Otherwise, e.cancel = true.
            e.Cancel = queryPairState() ? false : true;
            // If we don't have the steam ID, query app list to get it
            if (steamId == 0)
            {
                // If queryAppList is successful, don't cancel. Otherwise, e.cancel = true. 
                e.Cancel = queryAppList() ? false : true;
            }
        }

        /// <summary>
        /// Runs upon completion of checking pair state when the user presses "Start Streaming Steam!"
        /// </summary>
        private void streamBwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show("Error getting device pair state. Check the hostname and try again");
                Debug.WriteLine("Stream BW Error");
            }
            else if (e.Cancelled)
            {
                Debug.WriteLine("Stream BW Cancelled");
            }
            else
            {
                // Save the user's host input and send it to the streamframe page
                PhoneApplicationService.Current.State["host"] = host_textbox.Text;
                NavigationService.Navigate(new Uri("/StreamFrame.xaml?steamId=" + steamId, UriKind.Relative));
            }
            status_text.Text = ""; 
            Debug.WriteLine("Stream BW completed");
        }


        /// <summary>
        /// Pair with the hostname in the textbox
        /// </summary>
        private void pairBwDoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("Pairing ");
            // Create NvHttp object with the user input as the URL
            nv = new NvHttp((string)e.Argument);

            // Pair with the server. If queryAppList is successful, don't cancel the background worker. Otherwise, e.cancel = true. 
            e.Cancel = pair() ? false : true; 


            // If queryAppList is successful, don't cancel the background worker. Otherwise, e.cancel = true. 
            e.Cancel = queryAppList() ? false : true;

        }
       
        /// <summary>
        /// Runs once the background worker completes
        /// </summary>
        private void pairBwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Check to see if an error occurred in the background process.
            if (e.Error != null)
            {
                MessageBox.Show("Pairing error: " + e.Error.Message);
            }

            // Everything completed normally
            else
            {
                Debug.WriteLine("Pairing background Worker Successfully Completed");
            }
            status_text.Text = "";
        }

        #endregion Background Workers

        #region Persistent Settings
        /// <summary>
        /// Save page settings so the user doesn't have to select them each time she opens the app
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

            // Save fps radio button state
            if (!settings.Contains("fps"))
            {
                settings.Add("fps", _30fps_button.IsChecked);
            }
            else
            {
                settings["fps"] = _30fps_button.IsChecked;
            }

            // Save pixels radio button state
            if (!settings.Contains("pixels"))
            {
                settings.Add("pixels", _720p_button.IsChecked);
            }
            else
            {
                settings["pixels"] = _720p_button.IsChecked;
            }
            settings.Save();
        }

        /// <summary>
        /// Load persisted settings. Called on page load. 
        /// </summary>
        private void LoadSettings()
        {
            // Load hostname into the textbox, if any
            if (IsolatedStorageSettings.ApplicationSettings.Contains("hostname"))
            {
                host_textbox.Text = IsolatedStorageSettings.ApplicationSettings["hostname"] as string;
            }
            // Load fps radio button state
            if (IsolatedStorageSettings.ApplicationSettings.Contains("fps"))
            {
                _30fps_button.IsChecked = (bool)IsolatedStorageSettings.ApplicationSettings["fps"]; 
            }
            // Load fps radio button state
            if (IsolatedStorageSettings.ApplicationSettings.Contains("pixels"))
            {
                _720p_button.IsChecked = (bool)IsolatedStorageSettings.ApplicationSettings["pixels"];
            }
        }
        #endregion Persistent Settings

        #region Helper Methods

        /// <summary>
        /// Query the app list on the server to get the Steam App ID
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        private bool queryAppList()
        {
            XmlQuery appList = new XmlQuery(nv.baseUrl + "/applist?uniqueid=" + nv.GetDeviceName());
            // Error querying app list
            if (appList.GetErrorMessage() != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("App list query failed: " + appList.GetErrorMessage())));
                return false;
            }
            // App list query went well - try to get the steam ID
            else
            {
                string steamIdStr = appList.XmlAttribute("ID", appList.XmlAttributeElement("App"));
                // Steam ID lookup failed
                if (appList.GetErrorMessage() != null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Failed to get Steam ID: " + appList.GetErrorMessage())));
                    return false; 
                }
                // We're in the clear - save the Steam app ID
                else
                {
                    steamId = Convert.ToInt32(steamIdStr);
                    return true; 
                }
            }
        }

        /// <summary>
        /// Query the server to get the device pair state
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        bool queryPairState()
        {
            XmlQuery pairState = new XmlQuery(nv.baseUrl + "/pairstate?uniqueid=" + nv.GetDeviceName());
            if (pairState.GetErrorMessage() != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Failed to get pair state: " + pairState.GetErrorMessage())));
                return false; 
            }
                // Check if the device is paired by checking the XML attribute within the <paired> tag
            else if (String.Compare(pairState.XmlAttribute("paired"), "0") == 0)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Device not paired")));
                return false; 
            }
            return true; 
        }

        /// <summary>
        /// Pair with the server by hitting the pairing URL 
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        bool pair()
        {
            XmlQuery pairInfo = new XmlQuery(nv.baseUrl + "/pair?uniqueid=" + nv.GetDeviceName());
            if (pairInfo.GetErrorMessage() != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Pairing failed: " + pairInfo.GetErrorMessage())));
                return false;
            }
            // Session ID = 0; pairing failed
            else if (String.Compare(pairInfo.XmlAttribute("sessionid"), "0") == 0)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Pairing failed: Session ID = 0")));
                return false; 
            }
            return true; 
        }

        #endregion Helper Methods
    }
}