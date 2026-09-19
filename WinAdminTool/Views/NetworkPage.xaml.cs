using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinAdminTool.Views
{
    public sealed partial class NetworkPage : Page
    {
        private readonly List<NetworkAdapterInfo> _adapters = new();

        private readonly DispatcherTimer _statisticsTimer;

        private NetworkAdapterInfo? _selectedAdapter;

        private long _previousReceivedBytes;
        private long _previousSentBytes;
        private DateTime _previousStatisticsTime;

        public NetworkPage()
        {
            InitializeComponent();

            _statisticsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            _statisticsTimer.Tick += StatisticsTimer_Tick;

            LoadNetworkAdapters();
        }

        private void LoadNetworkAdapters()
        {
            _adapters.Clear();

            foreach (NetworkInterface networkInterface in
                     NetworkInterface.GetAllNetworkInterfaces())
            {
                _adapters.Add(new NetworkAdapterInfo
                {
                    Name = networkInterface.Name,
                    Description = networkInterface.Description,
                    Status = GetStatusText(networkInterface.OperationalStatus),
                    MacAddress = FormatMacAddress(
                        networkInterface.GetPhysicalAddress()),
                    Interface = networkInterface
                });
            }

            AdapterList.ItemsSource = _adapters;
        }

        private void AdapterList_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (AdapterList.SelectedItem is NetworkAdapterInfo adapter)
            {
                _selectedAdapter = adapter;

                ResetStatisticsBaseline();

                ShowAdapterDetails(adapter);

                _statisticsTimer.Start();
            }
            else
            {
                _selectedAdapter = null;
                _statisticsTimer.Stop();
            }
        }

        private void StatisticsTimer_Tick(
            object? sender,
            object e)
        {
            if (_selectedAdapter == null)
            {
                return;
            }

            UpdateTrafficStatistics(_selectedAdapter);
        }

        private void ShowAdapterDetails(NetworkAdapterInfo adapter)
        {
            NetworkInterface networkInterface = adapter.Interface;

            IPInterfaceProperties properties =
                networkInterface.GetIPProperties();

            // ---------------------------------------------------------
            // Allgemein
            // ---------------------------------------------------------

            DetailName.Text =
                $"Adapter: {adapter.Name}";

            DetailDescription.Text =
                $"Beschreibung: {adapter.Description}";

            DetailStatus.Text =
                $"Status: {adapter.Status}";

            DetailMacAddress.Text =
                $"MAC-Adresse: {adapter.MacAddress}";

            // ---------------------------------------------------------
            // IPv4 zurücksetzen
            // ---------------------------------------------------------

            DetailIpAddress.Text = "Adresse: –";
            DetailSubnetMask.Text = "Subnetzmaske: –";
            DetailGateway.Text = "Gateway: –";
            DetailDhcp.Text = "DHCP: –";

            // ---------------------------------------------------------
            // IPv6 zurücksetzen
            // ---------------------------------------------------------

            DetailIpv6Address.Text = "Adressen: –";
            DetailIpv6Gateway.Text = "Gateway: –";

            // ---------------------------------------------------------
            // DNS zurücksetzen
            // ---------------------------------------------------------

            DetailDns.Text = "Server: –";
            DetailDnsEnabled.Text = "DNS-Auflösung: –";

            // ---------------------------------------------------------
            // Adaptereigenschaften zurücksetzen
            // ---------------------------------------------------------

            DetailInterfaceType.Text = "Adaptertyp: –";
            DetailSpeed.Text = "Geschwindigkeit: –";
            DetailMtu.Text = "MTU: –";
            DetailMulticast.Text = "Multicast: –";
            DetailDynamicDns.Text = "Dynamisches DNS: –";

            // ---------------------------------------------------------
            // Datenverkehr zurücksetzen
            // ---------------------------------------------------------

            DetailReceived.Text = "Empfangen gesamt: –";
            DetailSent.Text = "Gesendet gesamt: –";
            DetailReceiveRate.Text = "Empfangsrate: –";
            DetailSendRate.Text = "Senderate: –";

            // ---------------------------------------------------------
            // Statistik zurücksetzen
            // ---------------------------------------------------------

            DetailReceiveErrors.Text =
                "Empfangsfehler: –";

            DetailSendErrors.Text =
                "Sendefehler: –";

            DetailReceivedDiscards.Text =
                "Verworfene Empfangspakete: –";

            DetailSentDiscards.Text =
                "Verworfene Sendepakete: –";

            // ---------------------------------------------------------
            // IPv4
            // ---------------------------------------------------------

            var ipv4Addresses = properties.UnicastAddresses
                .Where(x =>
                    x.Address.AddressFamily ==
                    AddressFamily.InterNetwork)
                .ToList();

            if (ipv4Addresses.Count > 0)
            {
                var ipv4 = ipv4Addresses[0];

                DetailIpAddress.Text =
                    $"Adresse: {ipv4.Address}";

                if (ipv4.IPv4Mask != null)
                {
                    DetailSubnetMask.Text =
                        $"Subnetzmaske: {ipv4.IPv4Mask}";
                }
            }

            var ipv4Gateway = properties.GatewayAddresses
                .Where(x =>
                    x.Address.AddressFamily ==
                    AddressFamily.InterNetwork)
                .Select(x => x.Address.ToString())
                .ToList();

            if (ipv4Gateway.Count > 0)
            {
                DetailGateway.Text =
                    $"Gateway: {string.Join(", ", ipv4Gateway)}";
            }

            // ---------------------------------------------------------
            // DHCP
            // ---------------------------------------------------------

            var dhcpServers = properties.DhcpServerAddresses
                .Where(x =>
                    x.AddressFamily ==
                    AddressFamily.InterNetwork)
                .Select(x => x.ToString())
                .ToList();

            if (dhcpServers.Count > 0)
            {
                DetailDhcp.Text =
                    $"DHCP: Aktiv\nServer: {string.Join(", ", dhcpServers)}";
            }
            else
            {
                DetailDhcp.Text =
                    "DHCP: Nicht erkannt";
            }

            // ---------------------------------------------------------
            // IPv6
            // ---------------------------------------------------------

            var ipv6Addresses = properties.UnicastAddresses
                .Where(x =>
                    x.Address.AddressFamily ==
                    AddressFamily.InterNetworkV6)
                .Select(x => x.Address.ToString())
                .ToList();

            if (ipv6Addresses.Count > 0)
            {
                DetailIpv6Address.Text =
                    $"Adressen: {string.Join(", ", ipv6Addresses)}";
            }

            var ipv6Gateway = properties.GatewayAddresses
                .Where(x =>
                    x.Address.AddressFamily ==
                    AddressFamily.InterNetworkV6)
                .Select(x => x.Address.ToString())
                .ToList();

            if (ipv6Gateway.Count > 0)
            {
                DetailIpv6Gateway.Text =
                    $"Gateway: {string.Join(", ", ipv6Gateway)}";
            }

            // ---------------------------------------------------------
            // DNS
            // ---------------------------------------------------------

            var dnsServers = properties.DnsAddresses
                .Select(x => x.ToString())
                .ToList();

            if (dnsServers.Count > 0)
            {
                DetailDns.Text =
                    $"Server: {string.Join(", ", dnsServers)}";
            }

            DetailDnsEnabled.Text =
                $"DNS-Auflösung: " +
                $"{(properties.IsDnsEnabled ? "Aktiv" : "Deaktiviert")}";

            // ---------------------------------------------------------
            // Adaptereigenschaften
            // ---------------------------------------------------------

            DetailInterfaceType.Text =
                $"Adaptertyp: " +
                $"{GetInterfaceTypeText(networkInterface.NetworkInterfaceType)}";

            if (networkInterface.Speed > 0)
            {
                DetailSpeed.Text =
                    $"Geschwindigkeit: " +
                    $"{FormatSpeed(networkInterface.Speed)}";
            }

            try
            {
                var ipv4Properties =
                    properties.GetIPv4Properties();

                if (ipv4Properties != null)
                {
                    DetailMtu.Text =
                        $"MTU: {ipv4Properties.Mtu} Bytes";
                }
            }
            catch
            {
                DetailMtu.Text =
                    "MTU: –";
            }

            DetailMulticast.Text =
                $"Multicast: " +
                $"{(networkInterface.SupportsMulticast ? "Unterstützt" : "Nicht unterstützt")}";

            DetailDynamicDns.Text =
                $"Dynamisches DNS: " +
                $"{(properties.IsDynamicDnsEnabled ? "Aktiv" : "Deaktiviert")}";

            // Erste Statistikwerte sofort anzeigen
            UpdateTrafficStatistics(adapter);
        }

        private void ResetStatisticsBaseline()
        {
            _previousReceivedBytes = 0;
            _previousSentBytes = 0;
            _previousStatisticsTime = DateTime.UtcNow;
        }

        private void UpdateTrafficStatistics(
            NetworkAdapterInfo adapter)
        {
            try
            {
                IPv4InterfaceStatistics statistics =
                    adapter.Interface.GetIPv4Statistics();

                long receivedBytes =
                    statistics.BytesReceived;

                long sentBytes =
                    statistics.BytesSent;

                DateTime now =
                    DateTime.UtcNow;

                double elapsedSeconds =
                    (now - _previousStatisticsTime).TotalSeconds;

                if (elapsedSeconds > 0 &&
                    _previousStatisticsTime != default)
                {
                    long receivedDifference =
                        receivedBytes - _previousReceivedBytes;

                    long sentDifference =
                        sentBytes - _previousSentBytes;

                    if (receivedDifference >= 0)
                    {
                        double receiveBitsPerSecond =
                            receivedDifference * 8.0 /
                            elapsedSeconds;

                        DetailReceiveRate.Text =
                            $"Empfangsrate: " +
                            $"{FormatSpeed(receiveBitsPerSecond)}";
                    }

                    if (sentDifference >= 0)
                    {
                        double sendBitsPerSecond =
                            sentDifference * 8.0 /
                            elapsedSeconds;

                        DetailSendRate.Text =
                            $"Senderate: " +
                            $"{FormatSpeed(sendBitsPerSecond)}";
                    }
                }

                _previousReceivedBytes =
                    receivedBytes;

                _previousSentBytes =
                    sentBytes;

                _previousStatisticsTime =
                    now;

                DetailReceived.Text =
                    $"Empfangen gesamt: " +
                    $"{FormatBytes(receivedBytes)}";

                DetailSent.Text =
                    $"Gesendet gesamt: " +
                    $"{FormatBytes(sentBytes)}";

                DetailReceiveErrors.Text =
                    $"Empfangsfehler: " +
                    $"{statistics.IncomingPacketsWithErrors:N0}";

                DetailSendErrors.Text =
                    $"Sendefehler: " +
                    $"{statistics.OutgoingPacketsWithErrors:N0}";

                DetailReceivedDiscards.Text =
                    $"Verworfene Empfangspakete: " +
                    $"{statistics.IncomingPacketsDiscarded:N0}";

                DetailSentDiscards.Text =
                    $"Verworfene Sendepakete: " +
                    $"{statistics.OutgoingPacketsDiscarded:N0}";
            }
            catch
            {
                DetailReceived.Text =
                    "Empfangen gesamt: Nicht verfügbar";

                DetailSent.Text =
                    "Gesendet gesamt: Nicht verfügbar";

                DetailReceiveRate.Text =
                    "Empfangsrate: Nicht verfügbar";

                DetailSendRate.Text =
                    "Senderate: Nicht verfügbar";
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes:N0} Bytes";
            }

            double value = bytes;

            string[] units =
            {
                "Bytes",
                "KB",
                "MB",
                "GB",
                "TB"
            };

            int unitIndex = 0;

            while (value >= 1024 &&
                   unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return $"{value:0.0} {units[unitIndex]}";
        }

        private static string FormatSpeed(
            double bitsPerSecond)
        {
            if (bitsPerSecond < 1_000)
            {
                return $"{bitsPerSecond:0} Bit/s";
            }

            double kilobits =
                bitsPerSecond / 1_000;

            if (kilobits < 1_000)
            {
                return $"{kilobits:0.0} KBit/s";
            }

            double megabits =
                bitsPerSecond / 1_000_000;

            if (megabits < 1_000)
            {
                return $"{megabits:0.0} MBit/s";
            }

            double gigabits =
                bitsPerSecond / 1_000_000_000;

            return $"{gigabits:0.0} GBit/s";
        }

        private static string GetStatusText(
            OperationalStatus status)
        {
            return status switch
            {
                OperationalStatus.Up =>
                    "Verbunden",

                OperationalStatus.Down =>
                    "Getrennt",

                OperationalStatus.Dormant =>
                    "Wartend",

                OperationalStatus.Unknown =>
                    "Unbekannt",

                OperationalStatus.LowerLayerDown =>
                    "Nicht verfügbar",

                _ =>
                    status.ToString()
            };
        }

        private static string FormatMacAddress(
            PhysicalAddress address)
        {
            byte[] bytes =
                address.GetAddressBytes();

            if (bytes.Length == 0)
            {
                return "–";
            }

            return string.Join(
                ":",
                bytes.Select(b =>
                    b.ToString("X2")));
        }

        private static string GetInterfaceTypeText(
            NetworkInterfaceType type)
        {
            return type switch
            {
                NetworkInterfaceType.Ethernet =>
                    "Ethernet",

                NetworkInterfaceType.Wireless80211 =>
                    "WLAN",

                NetworkInterfaceType.GigabitEthernet =>
                    "Gigabit Ethernet",

                NetworkInterfaceType.FastEthernetFx =>
                    "Fast Ethernet",

                NetworkInterfaceType.FastEthernetT =>
                    "Fast Ethernet",

                NetworkInterfaceType.Loopback =>
                    "Loopback",

                NetworkInterfaceType.Tunnel =>
                    "Tunnel",

                NetworkInterfaceType.Ppp =>
                    "PPP",

                NetworkInterfaceType.Wwanpp =>
                    "WWAN",

                NetworkInterfaceType.Wwanpp2 =>
                    "WWAN",

                NetworkInterfaceType.GenericModem =>
                    "Modem",

                NetworkInterfaceType.Unknown =>
                    "Unbekannt",

                _ =>
                    type.ToString()
            };
        }

        protected override void OnNavigatedFrom(
            Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            _statisticsTimer.Stop();

            base.OnNavigatedFrom(e);
        }

        private sealed class NetworkAdapterInfo
        {
            public string Name { get; set; } =
                string.Empty;

            public string Description { get; set; } =
                string.Empty;

            public string Status { get; set; } =
                string.Empty;

            public string MacAddress { get; set; } =
                string.Empty;

            public NetworkInterface Interface { get; set; } =
                null!;
        }
    }
}
