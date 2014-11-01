namespace Limelight
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.UI.Core;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    using Limelight.Streaming;
    using Limelight_common_binding;

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
            //await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => computerPicker.ItemsSource = computerList);
        }

        private int GetStreamWidth()
        {
            if (_720p_button.IsChecked.Value)
            {
                return 1280;
            }
            else
            {
                return 1920;
            }
        }

        private int GetStreamHeight()
        {
            if (_720p_button.IsChecked.Value)
            {
                return 720;
            }
            else
            {
                return 1080;
            }
        }

        private int GetStreamFps()
        {
            if (_60fps_button.IsChecked.Value)
            {
                return 60;
            }
            else
            {
                return 30;
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
            // TODO use a spinner to avoid the appearance of the app being unresponsive
            PairButton.IsEnabled = false;
            StreamButton.IsEnabled = false;
            _60fps_button.IsEnabled = false;
            _30fps_button.IsEnabled = false;
            _720p_button.IsEnabled = false;
            _1080p_button.IsEnabled = false;

            selected = (Computer)computerPicker.SelectedItem;

            // User hasn't selected a machine or selected a placeholder
            if (selected == null || selected.IpAddress == null)
            {
                var dialog = new MessageDialog("No machine selected", "Streaming Failed");
                await dialog.ShowAsync();         
            }
            else
            {
                status_text.Text = "Checking pair state...";

                byte[] aesKey = PairingCryptoHelpers.GenerateRandomBytes(16);

                // GameStream only uses 4 bytes of a 16 byte IV. Go figure.
                byte[] aesRiIndex = PairingCryptoHelpers.GenerateRandomBytes(4);
                byte[] aesIv = new byte[16];
                Array.ConstrainedCopy(aesRiIndex, 0, aesIv, 0, aesRiIndex.Length);

                LimelightStreamConfiguration config = new LimelightStreamConfiguration(
                    GetStreamWidth(),
                    GetStreamHeight(),
                    GetStreamFps(),
                    5000, // FIXME: Scale by resolution
                    1024,
                    aesKey, aesIv);

                StreamContext context = await ConnectionManager.StartStreaming(selected, config);
                if (context != null)
                {
                    this.Frame.Navigate(typeof(StreamFrame), context);
                }

                status_text.Text = "";
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
                status_text.Text = "";
                return; 
            }

            PairingManager p = new PairingManager(selected);
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

        private async void Quit_Game(object sender, RoutedEventArgs e)
        {
            // TODO need to make a UI element to display this text
            Task.Run(() => SpinnerBegin("Quitting...")).Wait();

            try
            {
                await SpinnerBegin("Quitting");
                Computer selected = (Computer)computerPicker.SelectedItem;
                NvHttp nv = new NvHttp(selected.IpAddress);
                XmlQuery quit = new XmlQuery(nv.BaseUrl + "/cancel?uniqueid=" + nv.GetUniqueId());
            }
            catch (Exception)
            {
                SpinnerEnd();
                Debug.WriteLine("Quitting failed");
                var dialog = new MessageDialog("Failed to quit", "Error");
                dialog.ShowAsync();
                return;
            }
            finally
            {
                // Turn off the spinner
                SpinnerEnd();
            }
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
