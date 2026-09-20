using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

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

    private void SfcButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/k sfc /scannow",
            UseShellExecute = true
        });
    }

    private void DismButton_Click(object sender, RoutedEventArgs e)
    {
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
}