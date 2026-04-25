using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Management;

namespace RiotDNS
{
    public class NetworkAdapterInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IPAddress { get; set; }
        public string MACAddress { get; set; }
        public bool IsPrimary { get; set; }
        public string AdapterType { get; set; }

        public override string ToString()
        {
            string primaryIndicator = IsPrimary ? " (Primary)" : "";
            string ipInfo = !string.IsNullOrEmpty(IPAddress) ? $" - {IPAddress}" : "";
            return $"{Name}{primaryIndicator}{ipInfo}";
        }
    }

    public class NetworkAdapterHelper
    {
        public static List<NetworkAdapterInfo> GetActiveAdapters()
        {
            var adapters = new List<NetworkAdapterInfo>();

            try
            {
                // Get network interfaces
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();

                // Get WMI adapters to match with ManagementObject
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();

                // Find primary adapter (one with default gateway)
                string primaryAdapterId = GetPrimaryAdapterId(moc);

                foreach (NetworkInterface ni in interfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    {
                        var ipProperties = ni.GetIPProperties();
                        string ipAddress = "No IP";

                        if (ipProperties.UnicastAddresses.Count > 0)
                        {
                            // Get IPv4 address
                            var ipv4 = ipProperties.UnicastAddresses
                                .FirstOrDefault(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                            if (ipv4 != null)
                            {
                                ipAddress = ipv4.Address.ToString();
                            }
                        }

                        // Find matching WMI adapter
                        ManagementObject wmiAdapter = null;
                        foreach (ManagementObject mo in moc)
                        {
                            if ((bool)mo["IPEnabled"])
                            {
                                string macAddress = mo["MACAddress"]?.ToString();
                                if (macAddress != null &&
                                    macAddress.Replace(":", "").Replace("-", "").ToLower() ==
                                    ni.GetPhysicalAddress().ToString().ToLower())
                                {
                                    wmiAdapter = mo;
                                    break;
                                }
                            }
                        }

                        string adapterId = wmiAdapter?["SettingID"]?.ToString() ?? Guid.NewGuid().ToString();

                        adapters.Add(new NetworkAdapterInfo
                        {
                            Id = adapterId,
                            Name = ni.Name,
                            Description = ni.Description,
                            IPAddress = ipAddress,
                            MACAddress = ni.GetPhysicalAddress().ToString(),
                            IsPrimary = adapterId == primaryAdapterId,
                            AdapterType = ni.NetworkInterfaceType.ToString()
                        });
                    }
                }

                // Sort: Primary first, then by name
                adapters = adapters
                    .OrderByDescending(a => a.IsPrimary)
                    .ThenBy(a => a.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                new Controller().LogWrite($"Error getting adapters: {ex.Message}");
            }

            return adapters;
        }

        private static string GetPrimaryAdapterId(ManagementObjectCollection moc)
        {
            foreach (ManagementObject mo in moc)
            {
                if ((bool)mo["IPEnabled"] && mo["DefaultIPGateway"] != null)
                {
                    var gateways = (string[])mo["DefaultIPGateway"];
                    if (gateways != null && gateways.Length > 0)
                    {
                        return mo["SettingID"]?.ToString();
                    }
                }
            }
            return null;
        }
    }
}