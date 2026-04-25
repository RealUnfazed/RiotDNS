using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using AlirezaPlus;

namespace RiotDNS
{
    public partial class MainWindow : Window
    {
        Controller controller = new Controller();
        Settings settings = new Settings();
        DNSService DNS = new DNSService();
        string SetMyDNS = String.Empty;
        private List<NetworkAdapterInfo> networkAdapters;
        private NetworkAdapterInfo selectedAdapter;

        public MainWindow()
        {
            InitializeComponent();
            InitializeExceptionHandlers();
            CheckAdminPrivileges();
            InitializeApp();
            LoadNetworkAdapters();
        }

        private void LoadNetworkAdapters()
        {
            try
            {
                networkAdapters = NetworkAdapterHelper.GetActiveAdapters();
                adapterCombo.ItemsSource = networkAdapters;

                if (networkAdapters.Count > 0)
                {
                    // Select primary adapter or first one
                    selectedAdapter = networkAdapters.FirstOrDefault(a => a.IsPrimary) ?? networkAdapters.First();
                    adapterCombo.SelectedItem = selectedAdapter;

                    if (networkAdapters.Count > 1)
                    {
                        statusText.Text = $"{networkAdapters.Count} adapters found. Select one.";
                    }
                    else
                    {
                        statusText.Text = $"Selected: {selectedAdapter.Name}";
                    }

                    // Enable toggle button if both adapter and DNS are selected
                    UpdateToggleButtonState();
                }
                else
                {
                    statusText.Text = "No active network adapters found";
                    Tg_btn.IsEnabled = false;
                }

                controller.LogWrite($"Loaded {networkAdapters.Count} network adapters");
            }
            catch (Exception ex)
            {
                controller.LogWrite($"Error loading adapters: {ex.Message}");
                statusText.Text = "Error loading adapters";
            }
        }

        private void InitializeExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    controller.LogWrite($"Unhandled Exception: {ex.Message}\n{ex.StackTrace}");
                }
                else
                {
                    controller.LogWrite($"Unhandled Exception: {e.ExceptionObject}");
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                foreach (var ex in e.Exception.InnerExceptions)
                {
                    controller.LogWrite($"Task Exception: {ex.Message}\n{ex.StackTrace}");
                }
                e.SetObserved();
            };

            controller.LogWrite("GLOBAL EXCEPTION HANDLERS INITIALIZED.");
        }

        private void CheckAdminPrivileges()
        {
            if (settings.CheckAdmin() == false)
            {
                MessageBoxResult result = MessageBox.Show("This application must be run as an administrator.", "ERROR CODE ( RD1 )", MessageBoxButton.OK, MessageBoxImage.Error);
                controller.LogWrite("APPLICATION ACCESS DENIED! ( RD1 )");
                if (result == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();
                }
            }
        }

        private void InitializeApp()
        {
            try
            {
                UserTracker.Track(settings.GetRDVersion(), settings.GetAppName());
                controller.LogWrite("APP STARTED v" + settings.GetRDVersion());
            }
            catch (Exception) { }

            try
            {
                if (settings.devMode != true)
                {
                    controller.LogWrite("THE UPDATER WAS CALLED");
                    AutoUpdater updater = new AutoUpdater(tagLbl);
                    updater.DoUpdate();
                }
                else
                {
                    controller.LogWrite("DEV MODE IS NOT ENABLED!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error checking for updates: " + ex.Message);
                controller.LogWrite("CRASH ON CHECKING DEV MODE : " + ex.Message + "( RD-1 )");
            }

            foreach (string item in settings.dnsServers)
            {
                dnsCombo.Items.Add(item);
            }
            dnsCombo.SelectedIndex = 0;
            controller.LogWrite("SERVERS IMPORTED");
        }

        private void AdapterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (adapterCombo.SelectedItem is NetworkAdapterInfo adapter)
            {
                selectedAdapter = adapter;
                statusText.Text = $"Selected: {adapter.Name} ({adapter.IPAddress})";
                UpdateToggleButtonState();
                controller.LogWrite($"Adapter selected: {adapter.Name}");
            }
        }

        private void UpdateToggleButtonState()
        {
            // Enable toggle button only if adapter is selected and DNS is selected
            Tg_btn.IsEnabled = selectedAdapter != null && dnsCombo.SelectedIndex >= 0;
        }

        private async void Tg_btn_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((bool)Tg_btn.IsChecked)
                {
                    // CONNECT
                    dnsCombo.IsEnabled = false;
                    adapterCombo.IsEnabled = false;

                    if (selectedAdapter == null)
                    {
                        MessageBox.Show("Please select a network adapter first.", "No Adapter Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Tg_btn.IsChecked = false;
                        return;
                    }

                    string dnsName = dnsCombo.SelectedItem.ToString();
                    tagLbl.Text = $"CONNECTING To {dnsName}";

                    // Set DNS on selected adapter
                    SetMyDNS = DNS.SetDNSForAdapter(dnsName, selectedAdapter.Id);

                    // Get ping
                    string pingResult = await controller.GetServerPing(dnsName);

                    tagLbl.Text = $"{SetMyDNS} - Ping: {pingResult}";
                    statusText.Text = $"Connected to {dnsName} on {selectedAdapter.Name}";
                }
                else
                {
                    // DISCONNECT
                    dnsCombo.IsEnabled = true;
                    adapterCombo.IsEnabled = true;

                    // Clear DNS on selected adapter
                    if (selectedAdapter != null)
                    {
                        DNS.ClearDNSForAdapter(selectedAdapter.Id);
                        tagLbl.Text = $"DISCONNECTED from {selectedAdapter.Name}";
                    }
                    else
                    {
                        DNS.ClearDNS();
                        tagLbl.Text = "DISCONNECTED";
                    }

                    statusText.Text = selectedAdapter != null ?
                        $"Selected: {selectedAdapter.Name}" :
                        "Select adapter";
                }

                controller.LogWrite($"MAIN BUTTON TOGGLED! State: {(bool)Tg_btn.IsChecked}");
            }
            catch (Exception ex)
            {
                controller.LogWrite($"Error in toggle button: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Tg_btn.IsChecked = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Auto-select first adapter on load
            if (adapterCombo.Items.Count > 0)
            {
                adapterCombo.SelectedIndex = 0;
            }
        }

        private void Close_App_Click(object sender, RoutedEventArgs e)
        {
            controller.LogWrite("APPLICATION CLOSED");
            Close();
        }

        // Optional: Refresh adapters button handler
        private void RefreshAdapters_Click(object sender, RoutedEventArgs e)
        {
            LoadNetworkAdapters();
        }
    }
}