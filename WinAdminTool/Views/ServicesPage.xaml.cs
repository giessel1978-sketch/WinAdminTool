using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using WinAdminTool.Models;

namespace WinAdminTool.Views;

public sealed partial class ServicesPage : Page
{
    public ObservableCollection<ServiceInfo> Services { get; } = new();

    public ObservableCollection<ServiceInfo> FilteredServices { get; } = new();

    public ServicesPage()
    {
        InitializeComponent();

        StatusFilter.SelectedIndex = 0;
        StartTypeFilter.SelectedIndex = 0;

        LoadServices();
    }

    private void LoadServices()
    {
        string? selectedServiceName = null;

        if (ServicesList.SelectedItem is ServiceInfo selectedService)
        {
            selectedServiceName = selectedService.Name;
        }

        Services.Clear();
        FilteredServices.Clear();

        try
        {
            using ManagementObjectSearcher searcher = new(
                "SELECT Name, DisplayName, State, StartMode FROM Win32_Service");

            foreach (ManagementObject service in searcher.Get())
            {
                string name = service["Name"]?.ToString() ?? string.Empty;
                string displayName = service["DisplayName"]?.ToString() ?? string.Empty;
                string state = service["State"]?.ToString() ?? string.Empty;
                string startMode = service["StartMode"]?.ToString() ?? string.Empty;

                string description = string.Empty;
                string startName = string.Empty;
                string pathName = string.Empty;

                using (RegistryKey? serviceKey = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Services\{name}"))
                {
                    if (serviceKey != null)
                    {
                        description =
                            serviceKey.GetValue("Description")?.ToString()
                            ?? string.Empty;

                        startName =
                            serviceKey.GetValue("ObjectName")?.ToString()
                            ?? string.Empty;

                        pathName =
                            serviceKey.GetValue("ImagePath")?.ToString()
                            ?? string.Empty;
                    }
                }

                description = ResolveResourceString(description);

                Services.Add(new ServiceInfo
                {
                    Name = name,
                    DisplayName = displayName,
                    Status = TranslateStatus(state),
                    StartType = TranslateStartType(startMode),
                    Description = description,
                    StartName = startName,
                    PathName = pathName,
                    ServiceState = state
                });
            }

            ApplyFilters();

            if (!string.IsNullOrWhiteSpace(selectedServiceName))
            {
                ServiceInfo? serviceToSelect =
                    FilteredServices.FirstOrDefault(
                        service => service.Name.Equals(
                            selectedServiceName,
                            StringComparison.OrdinalIgnoreCase));

                if (serviceToSelect != null)
                {
                    ServicesList.SelectedItem = serviceToSelect;
                }
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog(ex.Message);
        }
    }

    private void ApplyFilters()
    {
        string searchText = SearchBox?.Text?.Trim() ?? string.Empty;

        string selectedStatus =
            (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString()
            ?? "Alle";

        string selectedStartType =
            (StartTypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString()
            ?? "Alle";

        var filtered = Services.Where(service =>
            (
                string.IsNullOrWhiteSpace(searchText) ||
                service.Name.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase) ||
                service.DisplayName.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase)
            )
            &&
            (
                selectedStatus == "Alle" ||
                service.Status == selectedStatus
            )
            &&
            (
                selectedStartType == "Alle" ||
                service.StartType == selectedStartType
            )
        );

        FilteredServices.Clear();

        foreach (ServiceInfo service in filtered.OrderBy(s => s.DisplayName))
        {
            FilteredServices.Add(service);
        }
    }

    private void SearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void Filter_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        LoadServices();
    }

    private void ServicesList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (ServicesList.SelectedItem is not ServiceInfo service)
        {
            SelectedServiceName.Text = "Kein Dienst ausgewählt";

            ServiceDetailsPanel.Visibility =
                Visibility.Collapsed;

            NoSelectionText.Visibility =
                Visibility.Visible;

            UpdateServiceButtons(null);

            return;
        }

        SelectedServiceName.Text = service.Name;

        SelectedServiceDisplayName.Text =
            $"Anzeigename: {service.DisplayName}";

        SelectedServiceStatus.Text =
            $"Status: {service.Status}";

        SelectedServiceStartType.Text =
            $"Starttyp: {service.StartType}";

        SelectedServiceStartName.Text =
            $"Anmeldekonto: {service.StartName}";

        SelectedServiceDescription.Text =
            $"Beschreibung: {service.Description}";

        SelectedServicePath.Text =
            $"Pfad: {service.PathName}";

        ServiceDetailsPanel.Visibility =
            Visibility.Visible;

        NoSelectionText.Visibility =
            Visibility.Collapsed;

        UpdateServiceButtons(service);
    }

    private void UpdateServiceButtons(ServiceInfo? service)
    {
        if (service == null)
        {
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            RestartButton.IsEnabled = false;
            return;
        }

        bool isRunning =
            service.ServiceState.Equals(
                "Running",
                StringComparison.OrdinalIgnoreCase);

        bool isStopped =
            service.ServiceState.Equals(
                "Stopped",
                StringComparison.OrdinalIgnoreCase);

        bool isStarting =
            service.ServiceState.Equals(
                "Start Pending",
                StringComparison.OrdinalIgnoreCase);

        bool isStopping =
            service.ServiceState.Equals(
                "Stop Pending",
                StringComparison.OrdinalIgnoreCase);

        bool canStart =
            isStopped &&
            !service.StartType.Equals(
                "Deaktiviert",
                StringComparison.OrdinalIgnoreCase);

        bool canStop = isRunning;

        bool canRestart = isRunning;

        StartButton.IsEnabled = canStart;
        StopButton.IsEnabled = canStop;
        RestartButton.IsEnabled = canRestart;

        if (isStarting || isStopping)
        {
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            RestartButton.IsEnabled = false;
        }
    }

    private async void StartButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ServicesList.SelectedItem is not ServiceInfo service)
        {
            return;
        }

        try
        {
            using ServiceController controller =
                new(service.Name);

            controller.Refresh();

            if (controller.Status == ServiceControllerStatus.Running)
            {
                return;
            }

            if (controller.Status != ServiceControllerStatus.Stopped)
            {
                await ShowServiceActionError(
                    "Dienst kann nicht gestartet werden",
                    "Der Dienst befindet sich momentan in einem " +
                    "Zustand, in dem er nicht gestartet werden kann.");

                return;
            }

            SetActionButtonsEnabled(false);

            controller.Start();

            controller.WaitForStatus(
                ServiceControllerStatus.Running,
                TimeSpan.FromSeconds(30));

            LoadServices();
        }
        catch (Exception ex)
        {
            SetActionButtonsEnabled(true);

            await ShowServiceActionError(
                "Dienst konnte nicht gestartet werden",
                ex.Message);
        }
    }

    private async void StopButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ServicesList.SelectedItem is not ServiceInfo service)
        {
            return;
        }

        bool confirmed = await ShowConfirmationDialog(
            "Dienst stoppen",
            $"Möchtest du den Dienst „{service.DisplayName}“ wirklich stoppen?");

        if (!confirmed)
        {
            return;
        }

        try
        {
            using ServiceController controller =
                new(service.Name);

            controller.Refresh();

            if (controller.Status == ServiceControllerStatus.Stopped)
            {
                return;
            }

            if (!controller.CanStop)
            {
                await ShowServiceActionError(
                    "Dienst kann nicht gestoppt werden",
                    "Windows meldet, dass dieser Dienst nicht gestoppt werden kann.");

                return;
            }

            SetActionButtonsEnabled(false);

            controller.Stop();

            controller.WaitForStatus(
                ServiceControllerStatus.Stopped,
                TimeSpan.FromSeconds(30));

            LoadServices();
        }
        catch (Exception ex)
        {
            SetActionButtonsEnabled(true);

            await ShowServiceActionError(
                "Dienst konnte nicht gestoppt werden",
                ex.Message);
        }
    }

    private async void RestartButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (ServicesList.SelectedItem is not ServiceInfo service)
        {
            return;
        }

        bool confirmed = await ShowConfirmationDialog(
            "Dienst neu starten",
            $"Möchtest du den Dienst „{service.DisplayName}“ wirklich neu starten?");

        if (!confirmed)
        {
            return;
        }

        try
        {
            using ServiceController controller =
                new(service.Name);

            controller.Refresh();

            if (controller.Status != ServiceControllerStatus.Running)
            {
                return;
            }

            if (!controller.CanStop)
            {
                await ShowServiceActionError(
                    "Dienst kann nicht neu gestartet werden",
                    "Windows meldet, dass dieser Dienst nicht gestoppt werden kann.");

                return;
            }

            SetActionButtonsEnabled(false);

            controller.Stop();

            controller.WaitForStatus(
                ServiceControllerStatus.Stopped,
                TimeSpan.FromSeconds(30));

            controller.Start();

            controller.WaitForStatus(
                ServiceControllerStatus.Running,
                TimeSpan.FromSeconds(30));

            LoadServices();
        }
        catch (Exception ex)
        {
            SetActionButtonsEnabled(true);

            await ShowServiceActionError(
                "Dienst konnte nicht neu gestartet werden",
                ex.Message);
        }
    }

    private void SetActionButtonsEnabled(bool enabled)
    {
        if (!enabled)
        {
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            RestartButton.IsEnabled = false;

            return;
        }

        if (ServicesList.SelectedItem is ServiceInfo service)
        {
            UpdateServiceButtons(service);
        }
        else
        {
            UpdateServiceButtons(null);
        }
    }

    private async Task<bool> ShowConfirmationDialog(
        string title,
        string message)
    {
        ContentDialog dialog = new()
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Ja",
            CloseButtonText = "Abbrechen",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        ContentDialogResult result =
            await dialog.ShowAsync();

        return result == ContentDialogResult.Primary;
    }

    private async Task ShowServiceActionError(
        string title,
        string message)
    {
        ContentDialog dialog = new()
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private static string ResolveResourceString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (!value.StartsWith("@", StringComparison.Ordinal))
        {
            return value;
        }

        try
        {
            StringBuilder buffer = new(1024);

            int result = SHLoadIndirectString(
                value,
                buffer,
                buffer.Capacity,
                IntPtr.Zero);

            if (result == 0 && buffer.Length > 0)
            {
                return buffer.ToString();
            }
        }
        catch
        {
            // Bei einem Fehler ursprünglichen Wert beibehalten.
        }

        return value;
    }

    private static string TranslateStatus(string status)
    {
        return status switch
        {
            "Running" => "Wird ausgeführt",
            "Stopped" => "Beendet",
            "Paused" => "Pausiert",
            "Start Pending" => "Wird gestartet",
            "Stop Pending" => "Wird beendet",
            _ => status
        };
    }

    private static string TranslateStartType(string startType)
    {
        return startType switch
        {
            "Auto" => "Automatisch",
            "Manual" => "Manuell",
            "Disabled" => "Deaktiviert",
            "Boot" => "Boot",
            "System" => "System",
            _ => startType
        };
    }

    private async void ShowErrorDialog(string message)
    {
        ContentDialog dialog = new()
        {
            Title = "Fehler beim Laden der Dienste",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }

    [DllImport(
        "shlwapi.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern int SHLoadIndirectString(
        string pszSource,
        StringBuilder pszOutBuf,
        int cchOutBuf,
        IntPtr ppvReserved);
}