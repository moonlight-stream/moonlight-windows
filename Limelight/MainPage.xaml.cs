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
        private BackgroundWorker pairBw = new BackgroundWorker();
        private BackgroundWorker streamBw = new BackgroundWorker(); 
        /// <summary>
        /// Steam App ID
        /// </summary>
        private int steamId = 0; 

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

        #region Event Handlers
        /// <summary>
        /// Executed when the user presses "Start Streaming Steam!"
        /// </summary>
        private void StreamButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings(); 
            Debug.WriteLine("Start Streaming button pressed");
            streamBw.RunWorkerAsync(host_textbox.Text); 
        }

        /// <summary>
        /// Executed when the user presses "Pair"
        /// </summary>
        private void PairButton_Click(object sender, RoutedEventArgs e)
        {
            pairBw.RunWorkerAsync(host_textbox.Text);
            SaveSettings(); 
        }
        #endregion Event Handlers

        #region Persistent Settings
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
        /// Load persisted settings
        /// </summary>
        private void LoadSettings()
        {
            // Load hostname into the textbox, if any
            if (IsolatedStorageSettings.ApplicationSettings.Contains("hostname"))
            {
                host_textbox.Text = IsolatedStorageSettings.ApplicationSettings["hostname"] as string;
            }
        }
        #endregion Persistent Settings

        #region Background Workers

        /// <summary>
        /// Runs upon completion of checking pair state
        /// </summary>
        private void streamBwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Debug.WriteLine("BW done");
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
            Debug.WriteLine("Stream BW completed");
        }

        /// <summary>
        /// When the user presses "Start Streaming Steam", first check that they are paired in the background worker
        /// </summary>
        private void streamBwDoWork(object sender, DoWorkEventArgs e)
        {
            NvHttp nv = new NvHttp((string)e.Argument);
            XmlQuery pairState = new XmlQuery(nv.baseUrl + "/pairstate?uniqueid=" + nv.GetDeviceName());
            if (pairState.GetErrorMessage() != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Failed to get pair state")));
                e.Cancel = true; 
            }
            else if (String.Compare(pairState.XmlAttribute("paired"), "0") == 0)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Device not paired")));
                e.Cancel = true; 
            }
            // TODO This is gross repeated code - fix it
            if (steamId == 0)
            {
                XmlQuery appList = new XmlQuery(nv.baseUrl + "/applist?uniqueid=" + nv.GetDeviceName());
                // Error querying app list
                if (appList.GetErrorMessage() != null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("App list query failed: " + appList.GetErrorMessage())));
                    e.Cancel = true;
                }
                // App list query went well - try to get the steam ID
                else
                {
                    string steamIdStr = appList.XmlAttribute("ID", appList.XmlAttributeElement("App"));
                    // Steam ID failed
                    if (appList.GetErrorMessage() != null)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Failed to get Steam ID: " + appList.GetErrorMessage())));
                        e.Cancel = true;
                    }
                    // We're in the clear - save the Steam app ID
                    {
                        steamId = Convert.ToInt32(steamIdStr);
                    }
                }
            }                
        }

        /// <summary>
        /// Pair with the hostname in the textbox
        /// </summary>
        private void pairBwDoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("Pairing ");
            // Create NvHttp object with the user input as the URL
            NvHttp nv = new NvHttp((string)e.Argument);
            XmlQuery pairInfo = new XmlQuery(nv.baseUrl + "/pair?uniqueid=" + nv.GetDeviceName());
            if (pairInfo.GetErrorMessage() != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Pairing failed: " + pairInfo.GetErrorMessage())));
                e.Cancel = true;  
            }
            // Session ID = 0; pairing failed
            else if (String.Compare(pairInfo.XmlAttribute("sessionid"), "0") == 0 )
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Pairing failed: Session ID = 0")));
                e.Cancel = true; 
            }
                // Session ID is okay - try to get the app list
            else
            {
                XmlQuery appList = new XmlQuery(nv.baseUrl + "/applist?uniqueid=" + nv.GetDeviceName());
                // Error querying app list
                if (appList.GetErrorMessage() != null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("App list query failed: " + appList.GetErrorMessage())));
                    e.Cancel = true; 
                }
                    // App list query went well - try to get the steam ID
                else
                {
                    string steamIdStr = appList.XmlAttribute("ID", appList.XmlAttributeElement("App"));
                    // Steam ID failed
                    if (appList.GetErrorMessage() != null)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Failed to get Steam ID: " + appList.GetErrorMessage())));
                        e.Cancel = true; 
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
       
        /// <summary>
        /// Runs once the background worker completes
        /// </summary>
        void pairBwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
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
        }

        #endregion Background Workers
    }
}