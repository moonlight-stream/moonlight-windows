using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading;
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

        private const int MDNS_POLLING_INTERVAL = 5; 

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

        }

        /// <summary>
        /// Once all the page elements are loaded, run mDNS discovery
        /// </summary>
        private async void Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Loaded");
            await EnumerateEligibleMachines();
            
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
        private async void OnTimerTick(object sender, EventArgs e)
        {
            await EnumerateEligibleMachines();
            Dispatcher.BeginInvoke(() => computerPicker.ItemsSource = computerList);            
        }

        /// <summary>
        /// Executed when the user presses "Start Streaming Steam!"
        /// </summary>
        private async void StreamButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Start Streaming button pressed");

            // Stop enumerating machines while we're trying to check pair state
            mDnsTimer.Stop();
            SaveSettings();

            // Don't let the user mash the buttons
            PairButton.IsEnabled = false;
            StreamButton.IsEnabled = false;
            _60fps_button.IsEnabled = false;
            _30fps_button.IsEnabled = false;
            _720p_button.IsEnabled = false;
            _1080p_button.IsEnabled = false; 

            status_text.Text = "Checking pair state...";
            selected = (Computer)computerPicker.SelectedItem;

            // Check the pair state
            await StreamSetup(selected.IpAddress);
            Dispatcher.BeginInvoke(() => status_text.Text = "");                 
            mDnsTimer.Start(); 

            // User can use the buttons again
            PairButton.IsEnabled = true;
            StreamButton.IsEnabled = true;
            _60fps_button.IsEnabled = true;
            _30fps_button.IsEnabled = true;
            _720p_button.IsEnabled = true;
            _1080p_button.IsEnabled = true; 
        }

        /// <summary>
        /// Executed when the user presses "Pair"
        /// </summary>
        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop polling timer while we're pairing
            mDnsTimer.Stop(); 
            // Don't let the user mash the buttons
            // TODO check on an actual network
                PairButton.IsEnabled = false;
                StreamButton.IsEnabled = false; 
            
            
            
            SaveSettings(); 
            status_text.Text = "Pairing...";
            selected = (Computer)computerPicker.SelectedItem;
            await Pair(selected.IpAddress);

            // User can use the buttons again
            Dispatcher.BeginInvoke(() =>
            {
                status_text.Text = "";
                PairButton.IsEnabled = true;
                StreamButton.IsEnabled = true;
            }
            ); 

        }
        #endregion Event Handlers  

        #region Server Query

        /// <summary>
        /// When the user presses "Start Streaming Steam", first check that they are paired in the background worker
        /// </summary>
        private async Task StreamSetup(string uri)
        {
            try
            {
                nv = new NvHttp(uri);
            }
            catch (ArgumentNullException)
            {
                // TODO will not being able to await cause big problems here? 
                StreamSetupFailed();
                return;
            }

            // If device is already paired, return.             
            if (!await Task.Run(() => QueryPairState()))
            {
                await StreamSetupFailed();
                return;
            }

            // If we haven't cancelled and don't have the steam ID, query app list to get it
            if (steamId == 0)
            {
                // If queryAppList fails, return
                if (!await Task.Run(() => QueryAppList()))
                {
                    await StreamSetupFailed();
                    return;
                }
            }
            await StreamSetupComplete(); 
        }

        /// <summary>
        /// Runs upon successful completion of checking pair state when the user presses "Start Streaming Steam!"
        /// </summary>
        private async Task StreamSetupComplete()
        {
            // Save the user's host input and send it to the streamframe page
            PhoneApplicationService.Current.State["host"] = selected.IpAddress; 
            // TODO this navigation will need fixing for the new framework
            await Task.Run(() => NavigationService.Navigate(new Uri("/StreamFrame.xaml?steamId=" + steamId, UriKind.Relative)));                         
        }

        /// <summary>
        /// Runs if checking pair state fails
        /// </summary>
        private async Task StreamSetupFailed()
        {
            Debug.WriteLine("Stream setup failed");
            Dispatcher.BeginInvoke(() => MessageBox.Show("Device not paired"));       
        }

        /// <summary>
        /// Pair with the hostname in the textbox
        /// </summary>
        private async Task Pair(string uri)
        {
            Debug.WriteLine("Pairing ");
            // Create NvHttp object with the user input as the URL
            try
            {
                nv = new NvHttp(uri);
            }
            catch (ArgumentNullException)
            {
                Dispatcher.BeginInvoke(() => MessageBox.Show("Invalid hostname")); 
                return; 
            }

            // Hit the pairing server. If it fails, return.
            if (!await Task.Run(() => PairHelper()))
            {                
                return; 
            }

            // Query the app list from the server. If it fails, return
            if (!await Task.Run(() => QueryAppList()))
            {
                return;
            }
            // Otherwise, everything was successful
            Dispatcher.BeginInvoke(() =>            
                MessageBox.Show("Pairing Successful")); 
        }

        #endregion Server Queries

        #region Persistent Settings
        /// <summary>
        /// Save page settings so the user doesn't have to select them each time she opens the app
        /// </summary>
        private void SaveSettings()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            
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
            // todo ASYNC
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
        private bool PairHelper()
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
            // TODO save previous machines you've connected to
            // Make a local copy of the computer list
            // The UI thread will populate the listbox with computerList whenever it pleases, so we don't want it to take the one we're modifying
            List<Computer> computerListLocal = new List<Computer>(computerList); 

            // Ignore all computers we may have found in the past
            computerListLocal.Clear(); 
            Debug.WriteLine("Enumerating machines...");

            // If there's no network, save time and don't do the time-consuming mDNS 
            if (!DeviceNetworkInformation.IsNetworkAvailable)
            {
                Debug.WriteLine("Network not available");
            } // TODO Zeroconf will fail if you're not on WiFi. Check if device is on wifi? 
            else
            {
                // Let Zeroconf do its magic and find everything it can with mDNS
                ILookup<string, string> domains = null;
                try
                {
                    domains = await ZeroconfResolver.BrowseDomainsAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("EXCEPTION " + e.Message);
                }
                IReadOnlyList<IZeroconfHost> responses = null;
                try
                {
                    responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));

                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception in ZeroconfResolver.ResolverAsyc (Expected if BrowseDomainsAsync excepted): " + e.Message);
                }
                if (responses != null)
                {
                    // Go through every response we received and grab only the ones running nvstream
                    foreach (var resp in responses)
                    {
                        if (resp.Services.ContainsKey("_nvstream._tcp.local."))
                        {
                            Computer toAdd = new Computer(resp.DisplayName, resp.IPAddress);
                            // If we don't have the computer already, add it
                            if (!computerListLocal.Exists(x => x.IpAddress == resp.IPAddress))
                            {
                                computerListLocal.Add(toAdd);
                                Debug.WriteLine(resp);
                            }
                        }
                    }
                }    
            }                   

            // We're done messing with the list - it's okay for the UI thread to update it now
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