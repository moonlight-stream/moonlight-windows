namespace Limelight
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Navigation;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Class variables

        private const int MDNS_POLLING_INTERVAL = 5;

        private DispatcherTimer mDnsTimer = new DispatcherTimer();

        private static List<Computer> computerList = new List<Computer>();
        private Computer selected = null;
        private CoreDispatcher dispatcher;
        #endregion Class variables

        #region Constructor
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
        #endregion Constuctor

        #region Event Handlers
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Add anything here to prepare page for display
        }

        /// <summary>
        /// When we leave the page, stop mDNS polling
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Debug.WriteLine("Stopping mDNS");
            mDnsTimer.Stop();
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
            // TODO use a spinner to avoid the appearance of the app being unresponsive
            PairButton.IsEnabled = false;
            StreamButton.IsEnabled = false;
            _60fps_button.IsEnabled = false;
            _30fps_button.IsEnabled = false;
            _720p_button.IsEnabled = false;
            _1080p_button.IsEnabled = false;

            selected = (Computer)computerPicker.SelectedItem;

            // User hasn't selected a machine or selected a placeholder
            if (selected == null || String.IsNullOrWhiteSpace(selected.IpAddress))
            {
                var dialog = new MessageDialog("No machine selected", "Streaming Failed");
                await dialog.ShowAsync();
            } 
            else
            {
                await StreamSetup(selected);
            }            

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
            Computer selected = (Computer)computerPicker.SelectedItem;

            // User hasn't selected anything or selected the placeholder
            if (selected == null || selected.IpAddress == null)
            {
                var dialog = new MessageDialog("No machine selected", "Pairing Failed");
                await dialog.ShowAsync();
                return; 
            }
            if (String.IsNullOrWhiteSpace(selected.IpAddress))
            {
                var dialog = new MessageDialog("Invalid IP address", "Pairing Failed");
                await dialog.ShowAsync();
                return; 
            }

            nv = new NvHttp(selected.IpAddress);
            Pairing p = new Pairing(nv);
            // Stop polling timer while we're pairing
            mDnsTimer.Stop();

            SaveSettings();

            await p.Pair(selected);

            mDnsTimer.Stop(); 
        }

        /// <summary>
        /// When the computer picker is opened, stop the mDNS timer
        /// That way, the list box won't update while the user is picking one
        /// </summary>
        private void PickerOpened(object sender, object e)
        {
            mDnsTimer.Stop();
        }

        /// <summary>
        /// When the computer picker is closed, start the mDNS timer again. 
        /// </summary>
        private void PickerClosed(object sender, object e)
        {
            mDnsTimer.Start();
        }

        private async void QuitGame(object sender, RoutedEventArgs e)
        {
            // If we haven't used nv before, create it. 
            if(nv == null){
                try
                {
                    await SpinnerBegin("Quitting");
                    Computer selected = (Computer)computerPicker.SelectedItem;
                    nv = new NvHttp(selected.IpAddress);
                    await nv.ServerIPAddress();
                    
                    XmlQuery quit = new XmlQuery(nv.BaseUrl + "/cancel?uniqueid=" + nv.GetUniqueId());
                }
                catch (Exception ex)
                {
                    SpinnerEnd();
                    Debug.WriteLine("Stream setup failed");
                    var dialog = new MessageDialog(ex.Message, "Quit Game Failed");
                    dialog.ShowAsync();
                    return;
                }
                finally
                {
                    // Turn off the spinner
                    SpinnerEnd(); 
                }
            }
        }
        /// <summary>
        /// ListView Item selected event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemSelected(object sender, ItemClickEventArgs e)
        {
            // HACK on Windows Phone, clicking the item does not automatically select it
            // Revisit once Threshold APIs are released
            computerPicker.SelectedItem = e.ClickedItem;
        }


        #endregion Event Handlers  

        /// <summary>
        /// Start spinning the progress ring
        /// </summary>
        /// <param name="text">Text to display alongside the spinner</param>
        private async Task SpinnerBegin(string text)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(() =>
            {
                // Darken background
                uiGrid.Opacity = .5;

                // Disable select UI elements
                StreamButton.IsEnabled = false;
                PairButton.IsEnabled = false;

                // Show the spinner
                spinner.IsActive = true;
            }));
          
        }

        private void SpinnerEnd()
        {
            // Hide the spinner
            spinner.IsActive = false;

            // Return background to normal opacity
            uiGrid.Opacity = 1;

            // Enable UI elements
            StreamButton.IsEnabled = true;
            PairButton.IsEnabled = true;
            
        }


    }
}
