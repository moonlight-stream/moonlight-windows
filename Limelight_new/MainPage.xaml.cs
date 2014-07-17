using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace Limelight_new
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.Networking.Connectivity;
    using Windows.Storage;
    using Windows.UI.Core;
    using Windows.UI.Popups;
    using Zeroconf; 

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Class variables

        private const int MDNS_POLLING_INTERVAL = 5;

        private NvHttp nv;
        private int steamId = 0;
        private DispatcherTimer mDnsTimer = new DispatcherTimer();

        private static List<Computer> computerList = new List<Computer>();
        private Computer selected = null;
        private CoreDispatcher dispatcher;


        #endregion Class variables
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            LoadSettings();

            // Set up timer for mDNS polling
            mDnsTimer.Interval = TimeSpan.FromSeconds(MDNS_POLLING_INTERVAL);
            mDnsTimer.Tick += OnTimerTick;
        }

        #region Event Handlers
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
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

        /// <summary>
        /// Once all the page elements are loaded, run mDNS discovery
        /// </summary>
        private async void Loaded(object sender, object e)
        {
            Debug.WriteLine("Loaded");
            await EnumerateEligibleMachines();

            computerPicker.ItemsSource = computerList;
            // Start regularly polling for machines
            mDnsTimer.Start();            
        }


        /// <summary>
        /// When the timer ticks, poll for machines with mDNS
        /// </summary>
        private async void OnTimerTick(object sender, object e)
        {
            await EnumerateEligibleMachines();
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => computerPicker.ItemsSource = computerList);
        }

        /// <summary>
        /// Executed when the user presses "Start Streaming Steam!"
        /// </summary>
        private void StreamButton_Click(object sender, RoutedEventArgs e)
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
            // User hasn't selected a machine
            if (selected == null)
            {
                var dialog = new MessageDialog("No machine selected", "Streaming Failed");
                dialog.ShowAsync(); 
                status_text.Text = "";               
            }
            else
            {
                StreamSetup(selected.IpAddress);
            }
            // Check the pair state
            status_text.Text = "";
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
        private void PairButton_Click(object sender, RoutedEventArgs e)
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

            // User hasn't selected anything 
            if (selected == null)
            {
                var dialog = new MessageDialog("No machine selected", "Pairing Failed");
                dialog.ShowAsync(); 
            }
            else
            {
                Pair(selected.IpAddress);
            }

            // User can use the buttons again

            status_text.Text = "";
            PairButton.IsEnabled = true;
            StreamButton.IsEnabled = true;

            mDnsTimer.Start(); 
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
            catch (Exception)
            {
                // TODO will not being able to await cause problems here? 
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
            // TODO create some object for the parameter to pass that's not a linked list
            LinkedList<string> parameter = new LinkedList<string>();
            parameter.AddFirst(selected.IpAddress);
            parameter.AddLast(steamId.ToString());
            
            this.Frame.Navigate(typeof(StreamFrame), parameter);
        }

        /// <summary>
        /// Runs if checking pair state fails
        /// </summary>
        private async Task StreamSetupFailed()
        {
            Debug.WriteLine("Stream setup failed");
            var dialog = new MessageDialog("Device not paired"); 
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await dialog.ShowAsync());
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
            catch (Exception)
            {
                var dialog = new MessageDialog("Invalid Hostname");
                dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await dialog.ShowAsync());
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
            var successDialog = new MessageDialog("Pairing Successful");
            // TODO do I even need the dispatcher for these message boxes? 
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await successDialog.ShowAsync());
        }

        #endregion Server Queries

        #region Persistent Settings
        /// <summary>
        /// Save page settings so the user doesn't have to select them each time she opens the app
        /// </summary>
        private void SaveSettings()
        {
            var settings = ApplicationData.Current.RoamingSettings;

            // Save fps radio button state
            
            settings.Values["fps"] = _30fps_button.IsChecked;
            settings.Values["pixels"] = _720p_button.IsChecked;
        }

        /// <summary>
        /// Load persisted settings. Called on page load. 
        /// </summary>
        private void LoadSettings()
        {
            // Load fps radio button state
            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("fps"))
            {
                if ((bool)ApplicationData.Current.RoamingSettings.Values["fps"])
                {
                    _30fps_button.IsChecked = true;
                }
                else
                {
                    _60fps_button.IsChecked = true;
                }
            }
            // Load fps radio button state
            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("pixels"))
            {
                if ((bool)ApplicationData.Current.RoamingSettings.Values["pixels"])
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
        private async Task<bool> QueryAppList()
        {
            // todo ASYNC
            XmlQuery appList;
            string steamIdStr;
            try
            {
                appList = new XmlQuery(nv.baseUrl + "/applist?uniqueid=" + nv.GetDeviceName());
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Device not paired: " + e.Message);
                dialog.ShowAsync(); 
                return false;
            }
            // App list query went well - try to get the steam ID
            try
            {
                steamIdStr = await Task.Run(() => appList.XmlAttribute("ID", appList.XmlAttributeElement("App")));

            }
            catch (Exception e)
            {
                // Steam ID lookup failed
                var dialog = new MessageDialog("Failed to get Steam ID: " + e.Message);
                dialog.ShowAsync(); 
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
        private async Task<bool> QueryPairState()
        {
            XmlQuery pairState;
            try
            {
                pairState = new XmlQuery(nv.baseUrl + "/pairstate?uniqueid=" + nv.GetDeviceName());
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Failed to get pair state: " + e.Message);
                dialog.ShowAsync(); 
                return false;
            }

            // Check if the device is paired by checking the XML attribute within the <paired> tag
            if (String.Compare(pairState.XmlAttribute("paired"), "0") == 0)
            {
                var dialog = new MessageDialog("Device not paired");
                // TODO is this a vaild hack to get around the exception awaiting thing? 
                await dialog.ShowAsync(); 
                return false;
            }
            return true;
        }

        /// <summary>
        /// Pair with the server by hitting the pairing URL 
        /// </summary>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        private async Task<bool> PairHelper()
        {
            // Making the XML query to this URL does the actual pairing
            XmlQuery pairInfo;
            try
            {
                pairInfo = new XmlQuery(nv.baseUrl + "/pair?uniqueid=" + nv.GetDeviceName());
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog(e.Message, "Pairing Failed");
                dialog.ShowAsync(); 
                return false;
            }
            // Session ID = 0; pairing failed
            if (String.Compare(pairInfo.XmlAttribute("sessionid"), "0") == 0)
            {
                var dialog = new MessageDialog("Session ID = 0", "Pairing Failed");
                await dialog.ShowAsync(); 
                return false;
            }
            // Everything was successful
            var successDialog = new MessageDialog("Pairing completed successfully", "Pairing Completed");
            await successDialog.ShowAsync(); 
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
            if (!InternetAvailable)
            {
                Debug.WriteLine("Network not available");
            } 
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
                computerPicker.PlaceholderText = "No computers found";
            }
        }

        /// <summary>
        /// True if internet is available, false otherwise. 
        /// </summary>
        private bool InternetAvailable
        {
            get
            {
                var profiles = NetworkInformation.GetConnectionProfiles();
                var internetProfile = NetworkInformation.GetInternetConnectionProfile();
                return profiles.Any(s => s.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
                    || (internetProfile != null
                            && internetProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
            }
        }

        #endregion Helper Methods
    }
}
