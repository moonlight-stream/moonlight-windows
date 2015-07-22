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

        #region Event Handlers
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        private void OnNavigatedTo_Common(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                // If we are coming from AddPc, then add a PC to the list
                // HACK is there a way to get the name of the page you've come from? 
                Computer toAdd;
                try
                {
                    toAdd = (Computer)e.Parameter;
                }
                catch (Exception)
                {
                    // If that up there didn't work, then we didn't come from AddPC
                    return;
                }
                addedPCs.Add(toAdd);
            }
        }

        /// <summary>
        /// When we leave the page, stop mDNS polling
        /// </summary>
        private void OnNavigatedFrom_Common()
        {
            Debug.WriteLine("Stopping mDNS");
            mDnsTimer.Stop();
        }

        /// <summary>
        /// Once all the page elements are loaded, run mDNS discovery
        /// </summary>
        private async Task Loaded_Common()
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
        private async void OnTimerTick_Common()
        {
            Debug.WriteLine(computerPicker.SelectedIndex);
            await EnumerateEligibleMachines();
        }

        /// <summary>
        /// Executed when the user presses "Start Streaming Steam!"
        /// </summary>
        private async Task StreamButton_Click_Common()
        {
            Debug.WriteLine("Start Streaming button pressed");

            selected = (Computer)computerPicker.SelectedItem;

            // User hasn't selected a machine or selected a placeholder
            if (selected == null || String.IsNullOrWhiteSpace(selected.IpAddress))
            {
                DialogUtils.DisplayDialog(this.Dispatcher, "No machine selected", "Streaming Failed");
            }
            else
            {
                // Stop enumerating machines while we're trying to check pair state
                mDnsTimer.Stop();

                byte[] aesKey = PairingCryptoHelpers.GenerateRandomBytes(16);

                // GameStream only uses 4 bytes of a 16 byte IV. Go figure.
                byte[] aesRiIndex = PairingCryptoHelpers.GenerateRandomBytes(4);
                byte[] aesIv = new byte[16];
                Array.ConstrainedCopy(aesRiIndex, 0, aesIv, 0, aesRiIndex.Length);
                SettingsPage s = new SettingsPage();
                MoonlightStreamConfiguration config = new MoonlightStreamConfiguration(
                    s.GetStreamWidth(),
                    s.GetStreamHeight(),
                    s.GetStreamFps(),
                    10000, // FIXME: Scale by resolution
                    1024,
                    aesKey, aesIv);

                StreamContext context = await ConnectionManager.StartStreaming(this.Dispatcher, selected, config);
                if (context != null)
                {
                    this.Frame.Navigate(typeof(StreamFrame), context);
                }
            }
        }

        /// <summary>
        /// Executed when the user presses "Pair"
        /// </summary>
        private async Task PairButton_Click_Common()
        {
            Computer selected = (Computer)computerPicker.SelectedItem;

            // User hasn't selected anything or selected the placeholder
            if (selected == null || selected.IpAddress == null)
            {
                DialogUtils.DisplayDialog(this.Dispatcher, "No machine selected", "Pairing Failed");
                return;
            }

            PairingManager p = new PairingManager(selected);
            // Stop polling timer while we're pairing
            mDnsTimer.Stop();

            await p.Pair(this.Dispatcher, selected);

            mDnsTimer.Start();
        }

        /// <summary>
        /// When the computer picker is opened, stop the mDNS timer
        /// That way, the list box won't update while the user is picking one
        /// </summary>
        private void PickerOpened_Common()
        {
            mDnsTimer.Stop();
        }

        /// <summary>
        /// When the computer picker is closed, start the mDNS timer again. 
        /// </summary>
        private void PickerClosed_Common()
        {
            mDnsTimer.Start();
        }

        /// <summary>
        /// Quit Game Event Handler
        /// </summary>
        private async Task QuitGame_Common()
        {
            Computer selected = (Computer)computerPicker.SelectedItem;

            // User hasn't selected anything or selected the placeholder
            if (selected == null || selected.IpAddress == null)
            {
                DialogUtils.DisplayDialog(this.Dispatcher, "No machine selected", "Quit Failed");
                return;
            }

            PairingManager p = new PairingManager(selected);
            if (await p.QueryPairState() != true)
            {
                DialogUtils.DisplayDialog(this.Dispatcher, "Device not paired", "Quit Failed");
                return;
            }

            try
            {
                NvHttp nv = new NvHttp(selected.IpAddress);
                XmlQuery quit = new XmlQuery(nv.BaseUrl + "/cancel?uniqueid=" + nv.GetUniqueId());
                string cancelled = await quit.ReadXmlElement("cancel");
                if (cancelled == "1")
                {
                    DialogUtils.DisplayDialog(this.Dispatcher, "Successfully Quit Game", "Quit Game");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            DialogUtils.DisplayDialog(this.Dispatcher, "Unable to quit", "Quit Game");
        }

        /// <summary>
        /// Add PC clicked
        /// </summary>
        private void Add_AppBarButton_Click_Common()
        {
            // Navigate to the Add PC page
            this.Frame.Navigate(typeof(AddPC));
        }

        #endregion Event Handlers
    }
}
