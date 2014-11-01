namespace Limelight
{
    using Limelight.Streaming;

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
        Computer notFound = new Computer("No Computers Found", null);
        Computer noNetwork = new Computer("Network Unavailable", null);
        List<Computer> addedPCs = new List<Computer>();
        #region Enumeration
        /// <summary>
        /// Uses mDNS to enumerate the machines on the network eligible to stream from
        /// </summary>
        /// <returns></returns>
        private async Task EnumerateEligibleMachines()
        {
            computerList = new List<Computer>(); 
            // Make a local copy of the computer list
            // The UI thread will populate the listbox with computerList whenever it pleases, so we don't want it to take the one we're modifying
            List<Computer> computerListLocal = new List<Computer>(computerList);

            // Ignore all computers we may have found in the past
            computerListLocal.Clear();
            // Make sure we have the manually added PCs in here
            computerListLocal.AddRange(addedPCs);
            Debug.WriteLine("Enumerating machines...");

            // If there's no network, save time and don't do the time-consuming mDNS 
            if (!InternetAvailable)
            {
                if (computerListLocal.Count == 0 && !computerListLocal.Contains(noNetwork))
                {
                    computerListLocal.Add(noNetwork);
                }
                Debug.WriteLine("Network not available - skipping mDNS");
            }
            else
            {
                // Remove the placeholder
                if (computerListLocal.Contains(noNetwork))
                {
                    Debug.WriteLine("Removing \"no network\" placeholder");
                    computerListLocal.Remove(noNetwork);
                }


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
                if (domains != null)
                {
                    try
                    {
                        responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));

                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Exception in ZeroconfResolver.ResolverAsyc (Expected if BrowseDomainsAsync excepted): " + e.Message);
                    }
                }

                if (responses != null)
                {
                    // Remove the "not found" placeholder
                    if (computerList.Contains(notFound))
                    {
                        Debug.WriteLine("Removing \"not found\" placeholder");
                        computerList.Remove(notFound);
                    }

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

                // We're done messing with the list - it's okay for the UI thread to update it now
                Computer last = LoadComputer();
                if (last != null)
                {
                    // If we don't have the computer already, add it
                    if (!computerListLocal.Exists(x => x.IpAddress == last.IpAddress))
                    {
                        computerListLocal.Add(last);
                    }
                }

                // If no computers at all, say none are found.
                if (computerListLocal.Count == 0)
                {
                    Debug.WriteLine("Not Found");
                    computerListLocal.Add(notFound);
                }
            }

            computerList = computerListLocal;
            if (computerPicker.SelectedIndex == -1)
            {
                computerPicker.ItemsSource = computerList;
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