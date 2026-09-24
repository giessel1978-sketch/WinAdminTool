using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;

namespace WinAdminTool.Views;

public sealed partial class ToolsPage : Page
{
    public ToolsPage()
    {
        InitializeComponent();
    }

    private void DeviceManagerButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "devmgmt.msc",
            UseShellExecute = true
        });
    }

    private void ComputerManagementButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "compmgmt.msc",
            UseShellExecute = true
        });
    }

    private void DiskManagementButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "diskmgmt.msc",
            UseShellExecute = true
        });
    }

    private void SystemInformationButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "msinfo32.exe",
            UseShellExecute = true
        });
    }

    private void TaskManagerButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "taskmgr.exe",
            UseShellExecute = true
        });
    }

    private void EventViewerButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "eventvwr.msc",
            UseShellExecute = true
        });
    }

    private void CommandPromptButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            UseShellExecute = true
        });
    }

    private void PowerShellButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = true
        });
    }

    private void WindowsTerminalButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "wt.exe",
            UseShellExecute = true
        });
    }

    private void NetworkDiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "ms-settings:network-status",
            UseShellExecute = true
        });
    }

    private void FlushDnsButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k ipconfig /flushdns",
            UseShellExecute = true
        });
    }

    private async void RenewIpAddressButton_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = "IP-Adresse erneuern?",
            Content = "Die aktuelle IP-Adresse wird freigegeben und anschließend neu angefordert. Die Netzwerkverbindung kann dabei kurzzeitig unterbrochen werden.",
            PrimaryButtonText = "Ausführen",
            CloseButtonText = "Abbrechen",
            XamlRoot = XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k ipconfig /release && ipconfig /renew",
            UseShellExecute = true
        });
    }

    private async void ResetWinsockButton_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = "Winsock zurücksetzen?",
            Content = "Die Winsock-Konfiguration wird zurückgesetzt. Die Netzwerkverbindung kann dadurch beeinträchtigt werden. Ein Neustart von Windows kann erforderlich sein.",
            PrimaryButtonText = "Zurücksetzen",
            CloseButtonText = "Abbrechen",
            XamlRoot = XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k netsh winsock reset",
            UseShellExecute = true
        });
    }

    private async void ResetTcpIpButton_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = "TCP/IP zurücksetzen?",
            Content = "Der TCP/IP-Stack wird zurückgesetzt. Die Netzwerkverbindung kann dadurch beeinträchtigt werden. Ein Neustart von Windows kann erforderlich sein.",
            PrimaryButtonText = "Zurücksetzen",
            CloseButtonText = "Abbrechen",
            XamlRoot = XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k netsh int ip reset",
            UseShellExecute = true
        });
    }


    // ============================================================
    // Netzwerkdiagnose
    // ============================================================

    private void IpConfigButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k ipconfig /all",
            UseShellExecute = true
        });
    }

    private void PingButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k ping 8.8.8.8",
            UseShellExecute = true
        });
    }

    private void NslookupButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k nslookup",
            UseShellExecute = true
        });
    }

    private void RouteButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k route print",
            UseShellExecute = true
        });
    }

    private void ArpButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k arp -a",
            UseShellExecute = true
        });
    }

    private void NetstatButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k netstat -ano",
            UseShellExecute = true
        });
    }


    // ============================================================
    // System & Verwaltung
    // ============================================================

    private void ServicesManagementButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "services.msc",
            UseShellExecute = true
        });
    }

    private void SystemConfigurationButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "msconfig.exe",
            UseShellExecute = true
        });
    }

    private void RegistryEditorButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "regedit.exe",
            UseShellExecute = true
        });
    }

    private void LocalUsersButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "lusrmgr.msc",
            UseShellExecute = true
        });
    }

    private void SystemRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "rstrui.exe",
            UseShellExecute = true
        });
    }

    private async void LocalSecurityPolicyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(Environment.ExpandEnvironmentVariables(
            "%SystemRoot%\\System32\\secpol.msc")))
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Nicht verfügbar",
                Content = "Die lokale Sicherheitsrichtlinie ist in dieser Windows-Edition nicht verfügbar.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await dialog.ShowAsync();
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "secpol.msc",
            UseShellExecute = true
        });
    }

    private async void GroupPolicyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(Environment.ExpandEnvironmentVariables(
            "%SystemRoot%\\System32\\gpedit.msc")))
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Nicht verfügbar",
                Content = "Der Gruppenrichtlinieneditor ist in dieser Windows-Edition nicht verfügbar.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await dialog.ShowAsync();
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "gpedit.msc",
            UseShellExecute = true
        });
    }


    // ============================================================
    // Wartung & Reparatur
    // ============================================================

    private void SfcButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k sfc /scannow",
            UseShellExecute = true
        });
    }

    private void DismCheckHealthButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k DISM /Online /Cleanup-Image /CheckHealth",
            UseShellExecute = true
        });
    }

    private void DismScanHealthButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k DISM /Online /Cleanup-Image /ScanHealth",
            UseShellExecute = true
        });
    }

    private async void DismRestoreHealthButton_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = "DISM-Reparatur starten?",
            Content = "DISM überprüft das Windows-Komponentenabbild und versucht, erkannte Beschädigungen zu reparieren. Der Vorgang kann einige Zeit dauern.",
            PrimaryButtonText = "Reparatur starten",
            CloseButtonText = "Abbrechen",
            XamlRoot = XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k DISM /Online /Cleanup-Image /RestoreHealth",
            UseShellExecute = true
        });
    }

    private void ChkdskButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k chkdsk C:",
            UseShellExecute = true
        });
    }

    private void WindowsUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "ms-settings:windowsupdate",
            UseShellExecute = true
        });
    }
    private void AnalyzeComponentStoreButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k DISM /Online /Cleanup-Image /AnalyzeComponentStore",
            UseShellExecute = true
        });
    }

    private void UserTempButton_Click(object sender, RoutedEventArgs e)
    {
        string? tempPath = Environment.GetEnvironmentVariable("TEMP");

        if (!string.IsNullOrWhiteSpace(tempPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{tempPath}\"",
                UseShellExecute = true
            });
        }
    }

    private void WindowsTempButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "C:\\Windows\\Temp",
            UseShellExecute = true
        });
    }

    private void SoftwareDistributionButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "C:\\Windows\\SoftwareDistribution",
            UseShellExecute = true
        });
    }

    private void HostsFileButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "notepad.exe",
            Arguments = "C:\\Windows\\System32\\drivers\\etc\\hosts",
            UseShellExecute = true
        });
    }

    private void System32Button_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "C:\\Windows\\System32",
            UseShellExecute = true
        });
    }

    private void WindowsFolderButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "C:\\Windows",
            UseShellExecute = true
        });
    }
}
