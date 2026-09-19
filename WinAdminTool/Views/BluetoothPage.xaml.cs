using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;

namespace WinAdminTool.Views
{
    public sealed partial class BluetoothPage : Page
    {
        private readonly ObservableCollection<BluetoothDeviceInfo> _bluetoothDevices = new();

        public BluetoothPage()
        {
            InitializeComponent();

            BluetoothDevicesList.ItemsSource = _bluetoothDevices;

            _ = LoadBluetoothInformationAsync();
        }

        private async Task LoadBluetoothInformationAsync()
        {
            try
            {
                BluetoothDevicesStatus.Text = "Bluetooth-Geräte werden ermittelt...";

                await LoadBluetoothAdapterAsync();
                await LoadBluetoothDevicesAsync();

                BluetoothDevicesStatus.Text =
                    $"{_bluetoothDevices.Count} gekoppelte Bluetooth-Geräte gefunden.";
            }
            catch (Exception ex)
            {
                BluetoothDevicesStatus.Text =
                    $"Fehler beim Ermitteln der Bluetooth-Informationen: {ex.Message}";
            }
        }

        // ------------------------------------------------------------
        // Bluetooth-Adapter
        // ------------------------------------------------------------

        private async Task LoadBluetoothAdapterAsync()
        {
            try
            {
                var radios = await Radio.GetRadiosAsync();

                var bluetoothRadio = radios
                    .FirstOrDefault(r => r.Kind == RadioKind.Bluetooth);

                if (bluetoothRadio != null)
                {
                    AdapterName.Text = "Bluetooth";

                    AdapterStatus.Text = bluetoothRadio.State switch
                    {
                        RadioState.On => "Aktiv",
                        RadioState.Off => "Deaktiviert",
                        RadioState.Disabled => "Deaktiviert",
                        RadioState.Unknown => "Unbekannt",
                        _ => "Unbekannt"
                    };
                }
                else
                {
                    AdapterName.Text = "Kein Bluetooth-Adapter";
                    AdapterStatus.Text = "Nicht verfügbar";
                }

                await LoadBluetoothAdapterDetailsWithPowerShellAsync();
            }
            catch (Exception ex)
            {
                AdapterName.Text = "Bluetooth";
                AdapterStatus.Text = $"Fehler: {ex.Message}";
            }
        }

        private async Task LoadBluetoothAdapterDetailsWithPowerShellAsync()
        {
            try
            {
                string command =
                    "Get-CimInstance Win32_PnPEntity | " +
                    "Where-Object { $_.PNPClass -eq 'Bluetooth' } | " +
                    "Select-Object Name, Manufacturer, Status, PNPDeviceID, Service | " +
                    "ConvertTo-Csv -NoTypeInformation";

                var result = await RunPowerShellAsync(command);

                if (string.IsNullOrWhiteSpace(result))
                {
                    AdapterManufacturer.Text = "Unbekannt";
                    AdapterDeviceId.Text = "Nicht verfügbar";
                    return;
                }

                var lines = result
                    .Split(
                        new[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length < 2)
                {
                    AdapterManufacturer.Text = "Unbekannt";
                    AdapterDeviceId.Text = "Nicht verfügbar";
                    return;
                }

                string? selectedLine = null;

                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];

                    string lower = line.ToLowerInvariant();

                    if (lower.Contains("adapter") ||
                        lower.Contains("radio") ||
                        lower.Contains("intel") ||
                        lower.Contains("realtek") ||
                        lower.Contains("qualcomm") ||
                        lower.Contains("mediatek") ||
                        lower.Contains("broadcom"))
                    {
                        selectedLine = line;
                        break;
                    }
                }

                selectedLine ??= lines[1];

                string[] fields = ParseCsvLine(selectedLine);

                if (fields.Length >= 5)
                {
                    string name = fields[0];
                    string manufacturer = fields[1];
                    string status = fields[2];
                    string deviceId = fields[3];

                    if (!string.IsNullOrWhiteSpace(name))
                        AdapterName.Text = name;

                    AdapterManufacturer.Text =
                        string.IsNullOrWhiteSpace(manufacturer)
                            ? "Unbekannt"
                            : manufacturer;

                    AdapterStatus.Text =
                        string.IsNullOrWhiteSpace(status)
                            ? AdapterStatus.Text
                            : TranslatePnPStatus(status);

                    AdapterDeviceId.Text =
                        string.IsNullOrWhiteSpace(deviceId)
                            ? "Nicht verfügbar"
                            : deviceId;
                }
                else
                {
                    AdapterManufacturer.Text = "Unbekannt";
                    AdapterDeviceId.Text = "Nicht verfügbar";
                }
            }
            catch
            {
                AdapterManufacturer.Text = "Unbekannt";
                AdapterDeviceId.Text = "Nicht verfügbar";
            }
        }

        // ------------------------------------------------------------
        // Bluetooth-Geräte
        // ------------------------------------------------------------

        private async Task LoadBluetoothDevicesAsync()
        {
            _bluetoothDevices.Clear();

            var knownDeviceIds =
                new System.Collections.Generic.HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            // --------------------------------------------------------
            // Klassisches Bluetooth
            // --------------------------------------------------------

            try
            {
                string selector =
                    BluetoothDevice.GetDeviceSelectorFromPairingState(true);

                var devices = await DeviceInformation.FindAllAsync(
                    selector,
                    new[]
                    {
                        "System.Devices.Aep.IsConnected",
                        "System.Devices.Aep.IsPaired",
                        "System.Devices.Aep.Bluetooth.Le.IsConnectable"
                    },
                    DeviceInformationKind.AssociationEndpoint);

                foreach (var device in devices)
                {
                    if (ShouldIgnoreBluetoothDevice(device))
                        continue;

                    if (!knownDeviceIds.Add(device.Id))
                        continue;

                    bool connected = GetBooleanProperty(
                        device,
                        "System.Devices.Aep.IsConnected");

                    bool paired = GetBooleanProperty(
                        device,
                        "System.Devices.Aep.IsPaired");

                    string name = string.IsNullOrWhiteSpace(device.Name)
                        ? "Unbekanntes Bluetooth-Gerät"
                        : device.Name;

                    string deviceType = DetermineDeviceType(
                        name,
                        device.Id,
                        device.Properties);

                    _bluetoothDevices.Add(
                        new BluetoothDeviceInfo
                        {
                            Name = name,
                            Manufacturer = "Unbekannt",
                            Status = connected
                                ? "Verbunden"
                                : "Nicht verbunden",
                            DeviceId = device.Id,
                            Service = "Bluetooth",
                            ConnectionStatus = connected
                                ? "Verbunden"
                                : "Nicht verbunden",
                            PairingStatus = paired
                                ? "Gekoppelt"
                                : "Nicht gekoppelt",
                            DeviceType = deviceType
                        });
                }
            }
            catch
            {
                // Wenn klassisches Bluetooth nicht verfügbar ist,
                // versuchen wir trotzdem Bluetooth LE.
            }

            // --------------------------------------------------------
            // Bluetooth Low Energy
            // --------------------------------------------------------

            try
            {
                string selector =
                    BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);

                var devices = await DeviceInformation.FindAllAsync(
                    selector,
                    new[]
                    {
                        "System.Devices.Aep.IsConnected",
                        "System.Devices.Aep.IsPaired"
                    },
                    DeviceInformationKind.AssociationEndpoint);

                foreach (var device in devices)
                {
                    if (ShouldIgnoreBluetoothDevice(device))
                        continue;

                    if (!knownDeviceIds.Add(device.Id))
                        continue;

                    bool connected = GetBooleanProperty(
                        device,
                        "System.Devices.Aep.IsConnected");

                    bool paired = GetBooleanProperty(
                        device,
                        "System.Devices.Aep.IsPaired");

                    string name = string.IsNullOrWhiteSpace(device.Name)
                        ? "Unbekanntes Bluetooth-LE-Gerät"
                        : device.Name;

                    string deviceType = DetermineDeviceType(
                        name,
                        device.Id,
                        device.Properties);

                    _bluetoothDevices.Add(
                        new BluetoothDeviceInfo
                        {
                            Name = name,
                            Manufacturer = "Unbekannt",
                            Status = connected
                                ? "Verbunden"
                                : "Nicht verbunden",
                            DeviceId = device.Id,
                            Service = "Bluetooth LE",
                            ConnectionStatus = connected
                                ? "Verbunden"
                                : "Nicht verbunden",
                            PairingStatus = paired
                                ? "Gekoppelt"
                                : "Nicht gekoppelt",
                            DeviceType = deviceType
                        });
                }
            }
            catch
            {
                // Bluetooth LE ist optional.
            }

            // Alphabetisch sortieren.
            var sortedDevices = _bluetoothDevices
                .OrderBy(
                    d => d.Name,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            _bluetoothDevices.Clear();

            foreach (var device in sortedDevices)
            {
                _bluetoothDevices.Add(device);
            }
        }

        // ------------------------------------------------------------
        // Bluetooth-Geräte filtern
        // ------------------------------------------------------------

        private static bool ShouldIgnoreBluetoothDevice(
            DeviceInformation device)
        {
            if (device == null)
                return true;

            string name = device.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            string text = name.ToLowerInvariant();

            if (text == "bluetooth" ||
                text == "bluetooth adapter" ||
                text == "bluetooth radio" ||
                text.Contains("wireless bluetooth"))
            {
                return true;
            }

            return false;
        }

        // ------------------------------------------------------------
        // Gerätetyp bestimmen
        // ------------------------------------------------------------

        private static string DetermineDeviceType(
            string name,
            string deviceId,
            System.Collections.Generic.IReadOnlyDictionary<string, object> properties)
        {
            string text =
                $"{name} {deviceId}".ToLowerInvariant();

            // --------------------------------------------------------
            // Mäuse
            // --------------------------------------------------------

            if (text.Contains("mouse") ||
                text.Contains("maus") ||
                text.Contains("mx anywhere") ||
                text.Contains("mx master") ||
                text.Contains("mx ergo") ||
                text.Contains("logitech pebble") ||
                text.Contains("logitech m") ||
                text.Contains("magic mouse"))
            {
                return "Maus";
            }

            // --------------------------------------------------------
            // Tastaturen
            // --------------------------------------------------------

            if (text.Contains("keyboard") ||
                text.Contains("tastatur") ||
                text.Contains("logitech k") ||
                text.Contains("mx keys") ||
                text.Contains("magic keyboard"))
            {
                return "Tastatur";
            }

            // --------------------------------------------------------
            // Stifte
            // --------------------------------------------------------

            if (text.Contains("pen") ||
                text.Contains("stift"))
            {
                return "Stift";
            }

            // --------------------------------------------------------
            // Audio-Geräte
            // --------------------------------------------------------

            if (text.Contains("headset") ||
                text.Contains("headphone") ||
                text.Contains("headphones") ||
                text.Contains("kopfhörer") ||
                text.Contains("kopfhoerer") ||
                text.Contains("buds") ||
                text.Contains("earbuds") ||
                text.Contains("earbud") ||
                text.Contains("earphone") ||
                text.Contains("earphones") ||
                text.Contains("true wireless") ||
                text.Contains("wireless earbuds") ||
                text.Contains("wireless earbud") ||
                text.Contains("speaker") ||
                text.Contains("lautsprecher") ||
                text.Contains("soundbar") ||
                text.Contains("airpods") ||
                text.Contains("freebuds") ||
                text.Contains("galaxy buds") ||
                text.Contains("jabra") ||
                text.Contains("sennheiser") ||
                text.Contains("bose") ||
                text.Contains("sony wh-") ||
                text.Contains("wh-1000") ||
                text.Contains("wf-1000") ||
                text.Contains("momentum"))
            {
                return "Audio";
            }

            // --------------------------------------------------------
            // Controller / Gamepad
            // --------------------------------------------------------

            if (text.Contains("gamepad") ||
                text.Contains("controller") ||
                text.Contains("xbox") ||
                text.Contains("dualshock") ||
                text.Contains("dualsense"))
            {
                return "Controller";
            }

            // --------------------------------------------------------
            // Smartphone
            // --------------------------------------------------------

            if (text.Contains("phone") ||
                text.Contains("telefon") ||
                text.Contains("iphone") ||
                text.Contains("android") ||
                text.Contains("galaxy"))
            {
                return "Smartphone";
            }

            // --------------------------------------------------------
            // Drucker
            // --------------------------------------------------------

            if (text.Contains("printer") ||
                text.Contains("drucker"))
            {
                return "Drucker";
            }

            return "Bluetooth-Gerät";
        }

        // ------------------------------------------------------------
        // Auswahl eines Bluetooth-Gerätes
        // ------------------------------------------------------------

        private void BluetoothDevicesList_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (BluetoothDevicesList.SelectedItem
                is BluetoothDeviceInfo device)
            {
                DetailName.Text = device.Name;
                DetailType.Text = device.DeviceType;
                DetailConnection.Text = device.ConnectionStatus;
                DetailPairing.Text = device.PairingStatus;
                DetailDeviceId.Text = device.DeviceId;
            }
            else
            {
                DetailName.Text = "–";
                DetailType.Text = "–";
                DetailConnection.Text = "–";
                DetailPairing.Text = "–";
                DetailDeviceId.Text = "–";
            }
        }

        // ------------------------------------------------------------
        // PowerShell
        // ------------------------------------------------------------

        private static async Task<string> RunPowerShellAsync(
            string command)
        {
            return await Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments =
                        "-NoProfile -ExecutionPolicy Bypass -Command " +
                        $"\"{command}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using var process = new Process
                {
                    StartInfo = startInfo
                };

                process.Start();

                string output =
                    process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                return output.Trim();
            });
        }

        // ------------------------------------------------------------
        // Hilfsmethoden
        // ------------------------------------------------------------

        private static bool GetBooleanProperty(
            DeviceInformation device,
            string propertyName)
        {
            try
            {
                if (device.Properties.TryGetValue(
                    propertyName,
                    out object? value) &&
                    value is bool boolValue)
                {
                    return boolValue;
                }
            }
            catch
            {
                // Ignorieren und false zurückgeben.
            }

            return false;
        }

        private static string TranslatePnPStatus(string status)
        {
            return status switch
            {
                "OK" => "Aktiv",
                "Error" => "Fehler",
                "Degraded" => "Eingeschränkt",
                "Unknown" => "Unbekannt",
                "Pred Fail" => "Fehler",
                _ => status
            };
        }

        private static string[] ParseCsvLine(string line)
        {
            var values =
                new System.Collections.Generic.List<string>();

            var current = new StringBuilder();

            bool insideQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    insideQuotes = !insideQuotes;
                    continue;
                }

                if (c == ',' && !insideQuotes)
                {
                    values.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            values.Add(current.ToString().Trim());

            return values.ToArray();
        }

        // ------------------------------------------------------------
        // Datenmodell
        // ------------------------------------------------------------

        private sealed class BluetoothDeviceInfo
        {
            public string Name { get; set; } = string.Empty;

            public string Manufacturer { get; set; } = string.Empty;

            public string Status { get; set; } = string.Empty;

            public string DeviceId { get; set; } = string.Empty;

            public string Service { get; set; } = string.Empty;

            public string ConnectionStatus { get; set; } = string.Empty;

            public string PairingStatus { get; set; } = string.Empty;

            public string DeviceType { get; set; } = string.Empty;
        }
    }
}