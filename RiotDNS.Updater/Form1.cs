using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Xml;
using System.Diagnostics;

namespace RiotDNS.Updater
{
    public partial class RiotDNSUpdater : Form
    {
        public RiotDNSUpdater()
        {
            InitializeComponent();
        }

        private string updateXmlUrl;
        private string applicationPath;
        private string updateUrl;
        private byte[] updateBytes;
        private string updateXml;


        private void RiotDNSUpdater_Load(object sender, EventArgs e)
        {
            updateXmlUrl = "https://unfazed.ir/riotdns/content/info.xml";
            try
            {
                applicationPath = Path.GetDirectoryName(Application.ExecutablePath);
                XmlTextReader reader = new XmlTextReader(updateXmlUrl);
                while (reader.Read())
                {
                    WebClient webClient = new WebClient();

                    updateBytes = webClient.DownloadData(updateXmlUrl);
                    updateXml = Encoding.ASCII.GetString(updateBytes);

                    // Parse the manifest file to find the latest version number and other info
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(updateXml);
                    updateUrl = xmlDoc.SelectSingleNode("//url").InnerText;
                    webClient.DownloadFile(updateUrl, applicationPath + "\\" + "RiotDNS" + "-TempUpdate.zip");

                    if (File.Exists(applicationPath + "\\" + "RiotDNS.exe"))
                    {
                        File.Delete(applicationPath + "\\" + "RiotDNS.exe");
                        
                    }
                    if (File.Exists(applicationPath + "\\" + "AlirezaPlus.dll"))
                    {
                        File.Delete(applicationPath + "\\" + "AlirezaPlus.dll");
                    }

                    ZipFile.ExtractToDirectory(applicationPath + "\\" + "RiotDNS" + "-TempUpdate.zip", applicationPath);
                    Process.Start(applicationPath + "\\" + "RiotDNS.exe");
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                Application.Exit();
            }
        }
    }
}
