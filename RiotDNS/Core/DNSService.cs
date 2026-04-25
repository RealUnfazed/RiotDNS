using System.Management;
using System.Collections.Generic;
using System.Linq;

namespace RiotDNS
{
    public class DNSService
    {
        Controller controller = new Controller();
        Settings settings = new Settings();

        // Original method (for backward compatibility)
        public string SetDNS(string DnsName)
        {
            return SetDNSForAllAdapters(DnsName);
        }

        // Set DNS for specific adapter by ID
        public string SetDNSForAdapter(string DnsName, string adapterId)
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();

            bool success = false;
            string adapterName = "Unknown";

            foreach (ManagementObject mo in moc)
            {
                if ((bool)mo["IPEnabled"])
                {
                    string currentAdapterId = mo["SettingID"]?.ToString();

                    if (currentAdapterId == adapterId)
                    {
                        adapterName = mo["Description"]?.ToString() ?? "Unknown";

                        ManagementBaseObject objdns = mo.GetMethodParameters("SetDNSServerSearchOrder");
                        if (objdns != null)
                        {
                            string[] dnsServers = GetDnsServers(DnsName);
                            if (dnsServers != null)
                            {
                                objdns["DNSServerSearchOrder"] = dnsServers;
                                mo.InvokeMethod("SetDNSServerSearchOrder", objdns, null);
                                success = true;

                                controller.LogWrite($"CONNECTED TO {DnsName} on adapter: {adapterName}");
                            }
                        }
                        break;
                    }
                }
            }

            return success ? $"CONNECTED TO {DnsName} on {adapterName}" : "Failed to set DNS";
        }

        // Set DNS for all adapters (original behavior)
        public string SetDNSForAllAdapters(string DnsName)
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();

            int count = 0;
            foreach (ManagementObject mo in moc)
            {
                if ((bool)mo["IPEnabled"])
                {
                    ManagementBaseObject objdns = mo.GetMethodParameters("SetDNSServerSearchOrder");
                    if (objdns != null)
                    {
                        string[] dnsServers = GetDnsServers(DnsName);
                        if (dnsServers != null)
                        {
                            objdns["DNSServerSearchOrder"] = dnsServers;
                            mo.InvokeMethod("SetDNSServerSearchOrder", objdns, null);
                            count++;
                        }
                    }
                }
            }

            controller.LogWrite($"CONNECTED TO {DnsName} on {count} adapters");
            return $"CONNECTED TO {DnsName}";
        }

        // Clear DNS for specific adapter
        public void ClearDNSForAdapter(string adapterId)
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                if ((bool)mo["IPEnabled"])
                {
                    string currentAdapterId = mo["SettingID"]?.ToString();

                    if (currentAdapterId == adapterId)
                    {
                        mo.InvokeMethod("SetDNSServerSearchOrder", null);
                        controller.LogWrite($"DNS CLEARED for adapter: {adapterId}");
                        break;
                    }
                }
            }
        }

        // Clear all DNS (original method)
        public void ClearDNS()
        {
            ClearDNSForAllAdapters();
        }

        public void ClearDNSForAllAdapters()
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                if ((bool)mo["IPEnabled"])
                {
                    mo.InvokeMethod("SetDNSServerSearchOrder", null);
                }
            }
            controller.LogWrite("SYSTEM DNS SERVERS CLEARED");
        }

        // Helper method to get DNS servers
        private string[] GetDnsServers(string DnsName)
        {
            switch (DnsName)
            {
                case "Radar Game": return settings.radarAdr;
                case "Electro": return settings.electroAdr;
                case "Shecan": return settings.shecanAdr;
                case "Begzar": return settings.begzarAdr;
                case "Anti 403": return settings.anti403Adr;
                case "OpenDNS": return settings.opendnsAdr;
                case "CloudFlare": return settings.cloudflareAdr;
                case "Cloudflare": return settings.cloudflareAdr;
                case "Google": return settings.googleAdr;
                case "Quad 9": return settings.quad9Adr;
                case "KeepSolid": return settings.keepsolidAdr;
                case "Shelter Free": return settings.shelterfreeAdr;
                default: return null;
            }
        }
    }
}