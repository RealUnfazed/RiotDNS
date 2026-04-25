using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using RiotDNS.Properties;
using System.Windows.Controls;
using System.Runtime;

namespace RiotDNS
{
    
    public class AutoUpdater
    {
        Controller controller = new Controller();
        Settings Setting = new Settings();
        private TextBlock tagLbl;
        private string updateXmlUrl;
        private string applicationPath;
        private string applicationName;
        private Version currentVersion;
        private Version newVersion;
        private string updateUrl;
        private byte[] updateBytes;
        private string updateXml;
        public AutoUpdater(TextBlock tagLbl)
        {
            updateXmlUrl = "https://unfazed.ir/riotdns/content/info.xml";
            applicationPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            applicationName = "RiotDNS";
            currentVersion = new Version(Setting.GetRDVersion());
            this.tagLbl = tagLbl;
        }
        public void DoUpdate()
        {
            try
            {
                XmlTextReader reader = new XmlTextReader(updateXmlUrl);
                while (reader.Read())
                {
                    if (reader.Name == "version")
                    {
                        newVersion = new Version(reader.ReadString());
                        if (newVersion > currentVersion)
                        {
                            //updateUrl = reader.ReadString();
                            //updateUrl = updateUrl.Replace("%applicationPath%", applicationPath);
                            WebClient webClient = new WebClient();

                            updateBytes = webClient.DownloadData(updateXmlUrl);
                            updateXml = Encoding.ASCII.GetString(updateBytes);

                            // Parse the manifest file to find the latest version number and other info
                            XmlDocument xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(updateXml);
                            updateUrl = xmlDoc.SelectSingleNode("//url").InnerText;
                            controller.LogWrite("CLIENT USING OLD VERSION : v" + currentVersion);
                            controller.LogWrite("NEW VERSION DETECTED v" + newVersion);
                            Setting.buildType = "Old";
                            Process.Start(applicationPath + "\\" + applicationName + ".Updater.exe");
                            System.Windows.Application.Current.Shutdown();

                        } 
                        else if (newVersion < currentVersion)
                        {
                            if (File.Exists(applicationPath + "\\" + "RiotDNS" + "-TempUpdate.zip"))
                            {
                                File.Delete(applicationPath + "\\" + "RiotDNS" + "-TempUpdate.zip");
                            }
                            controller.LogWrite("CLIENT USING EARLY ACCESS VERSION : v" + currentVersion);
                            Setting.buildType = "Early Access";
                        }
                        else
                        {
                            if (File.Exists(applicationPath + "\\" + "RiotDNS" + "-TempUpdate.zip"))
                            {
                                File.Delete(applicationPath + "\\" + "RiotDNS" + "-TempUpdate.zip");
                            }
                            controller.LogWrite("CLIENT USING LATEST VERSION : v" + currentVersion);
                            Setting.buildType = "Release";
                        }
                        if (!File.Exists(applicationPath + "\\" + "AlirezaPlus.dll"))
                        {
                            controller.LogWrite("NECESSARY COMPONENT NOT FOUND : " + "AlirezaPlus.dll" + " | EXECUTING UPDATER...");
                            Process.Start(applicationPath + "\\" + applicationName + ".Updater.exe");
                            System.Windows.Application.Current.Shutdown();
                        }
                        tagLbl.Text = Setting.buildType + " " + "v" + Setting.GetRDVersion();
                    }
                }
            }
            catch (Exception ex)
            {
                
            }
        }

    }

}