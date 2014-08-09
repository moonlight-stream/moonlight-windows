namespace Limelight
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.Networking.Connectivity;
    using Windows.UI.Xaml.Controls;
    using Zeroconf;

    public sealed partial class MainPage : Page
    {
        #region Enumeration
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
                Debug.WriteLine("Network not available - skipping mDNS");
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
                    Debug.WriteLine("Browse Domains Async threw exception: " + e.Message);
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
                            Computer toAdd = new Computer(resp.DisplayName, resp.IPAddress, steamId);
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
            else if (computerList.Count == 1)
            {
                computerPicker.PlaceholderText = "1 computer found...";
            }
            else
            {
                computerPicker.PlaceholderText = computerList.Count + " computers found...";
            }
        }
        #endregion Enumeration

        #region Helpers
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
        #endregion Helpers
    }
}