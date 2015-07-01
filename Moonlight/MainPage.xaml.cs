namespace Moonlight
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    using Moonlight.Streaming;
    using Moonlight.Utils;
    using Moonlight_common_binding;
    using Windows.UI.Xaml.Controls.Primitives;

    /// <summary>
    /// Main UI page code - each method calls common code
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Constructor
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

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
            OnNavigatedTo_Common(e);
        }
        
        /// <summary>
        /// When we leave the page, stop mDNS polling
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            OnNavigatedFrom_Common(); 
        }

        /// <summary>
        /// Once all the page elements are loaded, run mDNS discovery
        /// </summary>
        private async void Loaded(object sender, object e)
        {
            await Loaded_Common();    
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
        /// Take the user to the Settings Page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        /// <summary>
        /// Executed when the user presses "Start Streaming Steam!"
        /// </summary>
        private async void StreamButton_Click(object sender, RoutedEventArgs e)
        {
            await StreamButton_Click_Common();
        }

        /// <summary>
        /// Executed when the user presses "Pair"
        /// </summary>
        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            await PairButton_Click_Common();
        }

        /// <summary>
        /// Quit Game Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QuitGame(object sender, RoutedEventArgs e)
        {
            QuitGame_Common();
        }

        /// <summary>
        /// Add PC click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Add_AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Add_AppBarButton_Click_Common(); 
        }

        /// <summary>
        /// ListView Item selected event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemSelected(object sender, ItemClickEventArgs e)
        {
            // HACK on Windows Phone clicking doesn't necessarily select the item
            computerPicker.SelectedItem = e.ClickedItem;
        }

        private void ShowFlyout(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
        }

        #endregion Event Handlers  
    }
}
