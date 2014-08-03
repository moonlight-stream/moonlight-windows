namespace Limelight_new
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.Storage;
    using Windows.UI.Core;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

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
            Debug.WriteLine(computerPicker.SelectedIndex);
            await EnumerateEligibleMachines();
            // Update the list only if the user hasn't selected anything - FIXME this is a workaround
            if (computerPicker.SelectedIndex < 0)
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => computerPicker.ItemsSource = computerList);
            }
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
            // User hasn't selected a machine
            if (selected == null)
            {
                var dialog = new MessageDialog("No machine selected", "Streaming Failed");
                await dialog.ShowAsync(); 
                status_text.Text = "";               
            }
            else
            {
                await StreamSetup(selected.IpAddress);
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
        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop polling timer while we're pairing
            mDnsTimer.Stop();

            SaveSettings();
            status_text.Text = "Pairing...";
            selected = (Computer)computerPicker.SelectedItem;
            // User hasn't selected anything 
            if (selected == null)
            {
                var dialog = new MessageDialog("No machine selected", "Pairing Failed");
                await dialog.ShowAsync();
                status_text.Text = "";  

            }
            else
            {
                // Pair with the selected machine
                await Pair(selected.IpAddress);
            }

            status_text.Text = ""; 
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
            // Pass the selected computer as the parameter
            //this.Frame.Navigate(typeof(StreamFrame), selected);
        }

        /// <summary>
        /// Runs if checking pair state fails
        /// </summary>
        private async Task StreamSetupFailed()
        {
            Debug.WriteLine("Stream setup failed");
            var dialog = new MessageDialog("Stream setup failed");
            await dialog.ShowAsync();
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
            XmlQuery appList;
            string steamIdStr;
            try
            {
                appList = new XmlQuery(nv.baseUrl + "/applist?uniqueid=" + nv.GetDeviceName());
            }
            catch (Exception e)
            {
                var dialog = new MessageDialog("Device not paired: " + e.Message, "App List Query Failed");
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
                var dialog = new MessageDialog("Failed to get Steam ID: " + e.Message, "Steam ID Lookup Failed");
                dialog.ShowAsync(); 
                return false;
            }

            // We're in the clear - save the Steam app ID
            steamId = Convert.ToInt32(steamIdStr);
            return true;
        }

        #endregion Helper Methods
    }
}
