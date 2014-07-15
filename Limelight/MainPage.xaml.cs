using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using Zeroconf;

namespace Limelight
{
    /// <summary>
    /// Main class for limelight-windows-phone
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        #region Class variables

        private const int MDNS_POLLING_INTERVAL = 6; 

        private BackgroundWorker pairBw = new BackgroundWorker();
        private BackgroundWorker streamBw = new BackgroundWorker();
        private NvHttp nv;
        private int steamId = 0;
        private DispatcherTimer mDnsTimer = new DispatcherTimer();

        private static List<Computer> computerList = new List<Computer>();
        private static List<string> placeholderText = new List<string> { "Discovery service is running..." };
        private Computer nullComputer = new Computer("No computers found", null);
        private Computer selected; 

        #endregion Class variables

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainPage class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            LoadSettings();
            computerPicker.ItemsSource = placeholderText; 

            // Set up timer for mDNS polling
            mDnsTimer.Interval = TimeSpan.FromSeconds(MDNS_POLLING_INTERVAL);
            mDnsTimer.Tick += OnTimerTick;

            // Set up background worker for pairing
            pairBw.WorkerSupportsCancellation = true;
            pairBw.DoWork += new DoWorkEventHandler(PairBwDoWork);
            pairBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PairBwRunWorkerCompleted);

            // Set up background worker for stream setup
            streamBw.WorkerSupportsCancellation = true;
            streamBw.DoWork += new DoWorkEventHandler(StreamBwDoWork);
            streamBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(StreamBwRunWorkerCompleted); 
        }

        /// <summary>
        /// Once all the page elements are loaded, run mDNS discovery
        /// </summary>
        private void Loaded(object sender, RoutedEventArgs e)
        {
            Task t = Task.Run(() => EnumerateEligibleMachines());
            t.Wait();
            computerPicker.ItemsSource = computerList;
            // Start regularly polling for machines
            mDnsTimer.Start();
        }

        /// <summary>
        /// When we leave the page, stop mDNS polling
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Debug.WriteLine("Stopping mDNS");
            mDnsTimer.Stop(); 
            base.OnNavigatedFrom(e);
        }

        #endregion Constructor

        #region Event Handlers
        /// <summary>
        /// When the timer ticks, poll for machines with mDNS
        /// </summary>
        private void OnTimerTick(object sender, EventArgs e)
        {
            Task t = Task.Run(() => EnumerateEligibleMachines());
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() => {
                computerPicker.ItemsSource = computerList;
            }));            
        }

        /// <summary>
        /// Executed when the user presses "Start Streaming Steam!"
        /// </summary>
        private void StreamButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            status_text.Text = "Checking pair state...";
            Debug.WriteLine("Start Streaming button pressed");
            selected = (Computer)computerPicker.SelectedItem;
            streamBw.RunWorkerAsync(selected.ipAddress); 
        }

        /// <summary>
        /// Executed when the user presses "Pair"
        /// </summary>
        private void PairButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings(); 
            status_text.Text = "Pairing...";
            selected = (Computer)computerPicker.SelectedItem;
            pairBw.RunWorkerAsync(selected.ipAddress);
        }
        #endregion Event Handlers  

        #region Background Workers


        /// <summary>
        /// When the user presses "Start Streaming Steam", first check that they are paired in the background worker
        /// </summary>
        private void StreamBwDoWork(object sender, DoWorkEventArgs e)
        {
            nv = new NvHttp((string)e.Argument);

            // If device is already paired, don't cancel. Otherwise, e.cancel = true.
            e.Cancel = QueryPairState() ? false : true;

            // If we haven't cancelled and don't have the steam ID, query app list to get it
            if (!e.Cancel && steamId == 0)
            {
                // If queryAppList is successful, don't cancel. Otherwise, e.cancel = true. 
                e.Cancel = QueryAppList() ? false : true;
            }
        }

        /// <summary>
        /// Runs upon completion of checking pair state when the user presses "Start Streaming Steam!"
        /// </summary>
        private void StreamBwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            status_text.Text = ""; 
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
                PhoneApplicationService.Current.State["host"] = selected.ipAddress; 
                NavigationService.Navigate(new Uri("/StreamFrame.xaml?steamId=" + steamId, UriKind.Relative));
            }
            Debug.WriteLine("Stream BW completed");
            
        }

        /// <summary>
        /// Pair with the hostname in the textbox
        /// </summary>
        private void PairBwDoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("Pairing ");
            // Create NvHttp object with the user input as the URL
            nv = new NvHttp((string)e.Argument);

            // Pair with the server. If queryAppList is successful, don't cancel the background worker. Otherwise, e.cancel = true. 
            e.Cancel = Pair() ? false : true; 


            // If queryAppList is successful, don't cancel the background worker. Otherwise, e.cancel = true. 
            if(!e.Cancel)
                e.Cancel = QueryAppList() ? false : true;

        }
       
        /// <summary>
        /// Runs once the background worker completes
        /// </summary>
        private void PairBwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            status_text.Text = "";
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
            // TODO unsure where hostname textbox will fit into the new UI with mDNS
            // Load hostname into the textbox, if any
            if (IsolatedStorageSettings.ApplicationSettings.Contains("hostname"))
            {
                host_textbox.Text = IsolatedStorageSettings.ApplicationSettings["hostname"] as string;
            }
            // Load fps radio button state
            if (IsolatedStorageSettings.ApplicationSettings.Contains("fps"))
            {
                if ((bool)IsolatedStorageSettings.ApplicationSettings["fps"])
                {
                    _30fps_button.IsChecked = true; 
                }
                else
                {
                    _60fps_button.IsChecked = true; 
                }
            }
            // Load fps radio button state
            if (IsolatedStorageSettings.ApplicationSettings.Contains("pixels"))
            {
                if ((bool)IsolatedStorageSettings.ApplicationSettings["pixels"])
                {
                    _720p_button.IsChecked = true;
                }
                else
                {
                    _1080p_button.IsChecked = true; 
                }
            }
        }
        #endregion Persistent Settings

        #region Helper Methods
        /// <summary>
        /// Query the app list on the server to get the Steam App ID
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        private bool QueryAppList()
        {
            XmlQuery appList;
            string steamIdStr;
            try
            {
               appList = new XmlQuery(nv.baseUrl + "/applist?uniqueid=" + nv.GetDeviceName());
            }
            catch (WebException e)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("App list query failed: " + e.Message)));
                return false;
            }
            // App list query went well - try to get the steam ID
            try
            {
                  steamIdStr = appList.XmlAttribute("ID", appList.XmlAttributeElement("App"));

            } catch (WebException e){
                // Steam ID lookup failed
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Failed to get Steam ID: " + e.Message)));
                return false; 
            }

                // We're in the clear - save the Steam app ID

            steamId = Convert.ToInt32(steamIdStr);
            return true; 
        }

        /// <summary>
        /// Query the server to get the device pair state
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        bool QueryPairState()
        {
            XmlQuery pairState;
            try
            {
                pairState = new XmlQuery(nv.baseUrl + "/pairstate?uniqueid=" + nv.GetDeviceName());
            }
            catch (WebException e)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Failed to get pair state: " + e.Message)));
                return false; 
            }
             
            // Check if the device is paired by checking the XML attribute within the <paired> tag
            if (String.Compare(pairState.XmlAttribute("paired"), "0") == 0)
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
        private bool Pair()
        {
            // Making the XML query to this URL does the actual pairing
            XmlQuery pairInfo;
            try 
            {
                pairInfo = new XmlQuery(nv.baseUrl + "/pair?uniqueid=" + nv.GetDeviceName());
            }
            catch (WebException e)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Pairing failed: " + e.Message)));
                return false;
            }
            // Session ID = 0; pairing failed
            if (String.Compare(pairInfo.XmlAttribute("sessionid"), "0") == 0)
            {
                Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Pairing failed: Session ID = 0")));
                return false; 
            }
            // Everything was successful
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Pairing completed successfully")));
            return true; 
        }
        /// <summary>
        /// Uses mDNS to enumerate the machines on the network eligible to stream from
        /// </summary>
        /// <returns></returns>
        private async Task EnumerateEligibleMachines()
        {
            // Make a local copy of the computer list
            // The UI thread will populate the listbox with computerList whenever it pleases, so we don't want it to take the one we're modifying
            List<Computer> computerListLocal = new List<Computer>(computerList); 

            // Ignore all computers we may have found in the past
            computerListLocal.Clear(); 
            Debug.WriteLine("Enumerating machines...");

            // Let Zeroconf do its magic and find everything it can with mDNS
            ILookup<string, string> domains = await ZeroconfResolver.BrowseDomainsAsync();
            var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));

            // Go through every response we received and grab only the ones running nvstream
            foreach (var resp in responses)
            { 
                if (resp.Services.ContainsKey("_nvstream._tcp.local."))
                {
                    Computer toAdd = new Computer(resp.DisplayName, resp.IPAddress);                   
                    if (!computerListLocal.Exists(x => x.ipAddress == resp.IPAddress))
                    {
                        computerListLocal.Add(toAdd);
                        Debug.WriteLine(resp);
                    }                    
                }
            }

            // We're done messing with the list - it's okay for the UI thread to take now
            computerList = computerListLocal; 
            if (computerList.Count == 0)
            {
                computerList.Add(nullComputer);
                Debug.WriteLine("None found");
            }
        }
        #endregion Helper Methods
    }
}