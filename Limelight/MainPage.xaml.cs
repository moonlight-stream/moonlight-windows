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
            if (steamId != 0)
            {
                // Save the user's host input and send it to the streamframe page
                PhoneApplicationService.Current.State["host"] = host_textbox.Text;
                NavigationService.Navigate(new Uri("/StreamFrame.xaml?steamId="+steamId, UriKind.Relative));
            }
            else
            {
                MessageBox.Show("Device not paired correctly: Steam not found");
            }
        }

        /// <summary>
        /// Executed when the user presses "Pair"
        /// </summary>
        private void PairButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings(); 
            bw.RunWorkerAsync(host_textbox.Text); 
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
            if (String.Compare(pairInfo.XmlAttribute("sessionid"), "0") == 0 )
            {
                MessageBox.Show("Pairing Failed");
                bw.CancelAsync();
            }
            else
            {
                XmlQuery appList = new XmlQuery(nv.baseUrl + "/applist?uniqueid=" + nv.GetDeviceName());
                string steamIdStr = appList.XmlAttributeFromElement("ID", appList.XmlAttributeElement("App"));
                steamId = Convert.ToInt32(steamIdStr);
                Debug.WriteLine(steamId);
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