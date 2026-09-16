using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace WinAdminTool.Views
{
    public sealed partial class OverviewPage : Page
    {
        public OverviewPage()
        {
            this.InitializeComponent();

            LoadSystemInformation();
        }

        private void LoadSystemInformation()
        {
            // Computername
            ComputerNameText.Text = Environment.MachineName;

            if (OperatingSystem.IsWindows())
            {
                WindowsVersionText.Text = GetWindowsDisplayVersion();
                WindowsBuildText.Text = GetWindowsBuild();
                WindowsArchitectureText.Text =
                    RuntimeInformation.OSArchitecture.ToString();

                WindowsEditionText.Text = GetWindowsEdition();
            }
            else
            {
                WindowsEditionText.Text = "Unbekannt";
                WindowsVersionText.Text = "Unbekannt";
                WindowsBuildText.Text = "Unbekannt";
                WindowsArchitectureText.Text = "Unbekannt";
            }
        }

        private string GetWindowsEdition()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

                if (key != null)
                {
                    string? productName =
                        key.GetValue("ProductName") as string;

                    object? currentBuildObject =
                        key.GetValue("CurrentBuild");

                    if (currentBuildObject != null &&
                        int.TryParse(
                            currentBuildObject.ToString(),
                            out int build))
                    {
                        string edition = "Pro";

                        if (!string.IsNullOrWhiteSpace(productName))
                        {
                            if (productName.Contains(
                                "Home",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                edition = "Home";
                            }
                            else if (productName.Contains(
                                "Enterprise",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                edition = "Enterprise";
                            }
                            else if (productName.Contains(
                                "Education",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                edition = "Education";
                            }
                        }

                        if (build >= 22000)
                        {
                            return $"Windows 11 {edition}";
                        }

                        return $"Windows 10 {edition}";
                    }
                }
            }
            catch
            {
                // Falls der Registry-Zugriff fehlschlägt.
            }

            return "Unbekannt";
        }

        private string GetWindowsDisplayVersion()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

                string? displayVersion =
                    key?.GetValue("DisplayVersion") as string;

                if (!string.IsNullOrWhiteSpace(displayVersion))
                {
                    return displayVersion;
                }
            }
            catch
            {
                // Falls der Registry-Zugriff fehlschlägt.
            }

            return "Unbekannt";
        }

        private string GetWindowsBuild()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

                string? currentBuild =
                    key?.GetValue("CurrentBuild") as string;

                string? ubr =
                    key?.GetValue("UBR")?.ToString();

                if (!string.IsNullOrWhiteSpace(currentBuild))
                {
                    if (!string.IsNullOrWhiteSpace(ubr))
                    {
                        return $"{currentBuild}.{ubr}";
                    }

                    return currentBuild;
                }
            }
            catch
            {
                // Falls der Registry-Zugriff fehlschlägt.
            }

            return "Unbekannt";
        }
    }
}