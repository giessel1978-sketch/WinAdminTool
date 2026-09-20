using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace WinAdminTool.Views;

public sealed partial class SecurityPage : Page
{
    public SecurityPage()
    {
        InitializeComponent();

        Loaded += SecurityPage_Loaded;
    }

    private async void SecurityPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadSecurityInformationAsync();
    }

    private async Task LoadSecurityInformationAsync()
    {
        DefenderInfo defender =
            await GetDefenderInformationAsync();

        FirewallInfo firewall =
            await GetFirewallInformationAsync();

        SystemSecurityInfo systemSecurity =
            GetSystemSecurityInformation();

        // ============================================================
        // MICROSOFT DEFENDER
        // ============================================================

        if (!defender.Available)
        {
            DefenderStatusTextBlock.Text =
                "Nicht verfügbar";

            DefenderDetailsTextBlock.Text =
                defender.ErrorMessage ??
                "Keine Informationen verfügbar.";
        }
        else
        {
            DefenderStatusTextBlock.Text =
                defender.RealTimeProtection
                    ? "Aktiv"
                    : "Deaktiviert";

            DefenderDetailsTextBlock.Text =
                $"Antivirenschutz: " +
                $"{(defender.AntivirusEnabled ? "Aktiv" : "Deaktiviert")}\n" +
                $"Antispyware: " +
                $"{(defender.AntispywareEnabled ? "Aktiv" : "Deaktiviert")}";
        }

        // ============================================================
        // WINDOWS-FIREWALL
        // ============================================================

        if (!firewall.Available)
        {
            FirewallStatusTextBlock.Text =
                "Nicht verfügbar";

            FirewallDetailsTextBlock.Text =
                firewall.ErrorMessage ??
                "Keine Informationen verfügbar.";
        }
        else
        {
            bool firewallEnabled =
                firewall.DomainEnabled ||
                firewall.PrivateEnabled ||
                firewall.PublicEnabled;

            FirewallStatusTextBlock.Text =
                firewallEnabled
                    ? "Aktiv"
                    : "Deaktiviert";

            FirewallDetailsTextBlock.Text =
                $"Domäne: " +
                $"{(firewall.DomainEnabled ? "Aktiv" : "Deaktiviert")}\n" +
                $"Privat: " +
                $"{(firewall.PrivateEnabled ? "Aktiv" : "Deaktiviert")}\n" +
                $"Öffentlich: " +
                $"{(firewall.PublicEnabled ? "Aktiv" : "Deaktiviert")}";
        }

        // ============================================================
        // WINDOWS-SICHERHEIT
        // ============================================================

        WindowsSecurityStatusTextBlock.Text =
            systemSecurity.GetOverallStatus();

        WindowsSecurityDetailsTextBlock.Text =
            $"UAC: {systemSecurity.UacStatus}\n" +
            $"Secure Boot: {systemSecurity.SecureBootStatus}\n" +
            $"TPM: {systemSecurity.TpmStatus}";

        // ============================================================
        // SICHERHEITSDETAILS
        // ============================================================

        RealTimeProtectionTextBlock.Text =
            defender.Available
                ? defender.RealTimeProtection
                    ? "Aktiviert"
                    : "Deaktiviert"
                : "-";

        AntivirusTextBlock.Text =
            defender.Available
                ? defender.AntivirusEnabled
                    ? "Aktiviert"
                    : "Deaktiviert"
                : "-";

        LastScanTextBlock.Text =
            defender.Available
                ? defender.LastScan
                : "-";

        SignatureVersionTextBlock.Text =
            defender.Available
                ? defender.SignatureVersion
                : "-";

        FirewallDomainTextBlock.Text =
            firewall.Available
                ? firewall.DomainEnabled
                    ? "Aktiviert"
                    : "Deaktiviert"
                : "-";

        FirewallPrivateTextBlock.Text =
            firewall.Available
                ? firewall.PrivateEnabled
                    ? "Aktiviert"
                    : "Deaktiviert"
                : "-";

        FirewallPublicTextBlock.Text =
            firewall.Available
                ? firewall.PublicEnabled
                    ? "Aktiviert"
                    : "Deaktiviert"
                : "-";

        SystemProtectionTextBlock.Text =
            $"UAC: {systemSecurity.UacStatus}\n" +
            $"Secure Boot: {systemSecurity.SecureBootStatus}\n" +
            $"TPM: {systemSecurity.TpmStatus}";
    }

    // ================================================================
    // MICROSOFT DEFENDER
    // ================================================================

    private async Task<DefenderInfo> GetDefenderInformationAsync()
    {
        try
        {
            string script =
                "Get-MpComputerStatus | " +
                "Select-Object " +
                "RealTimeProtectionEnabled," +
                "AntivirusEnabled," +
                "AntispywareEnabled," +
                "AntivirusSignatureVersion," +
                "QuickScanEndTime," +
                "FullScanEndTime | " +
                "ConvertTo-Csv -NoTypeInformation";

            string output =
                await RunPowerShellAsync(script);

            if (string.IsNullOrWhiteSpace(output))
            {
                return new DefenderInfo
                {
                    Available = false,
                    ErrorMessage =
                        "Microsoft Defender lieferte keine Daten."
                };
            }

            string[] lines =
                output.Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
            {
                return new DefenderInfo
                {
                    Available = false,
                    ErrorMessage =
                        "Microsoft Defender lieferte keine " +
                        "verwertbaren Daten."
                };
            }

            string[] values =
                ParseCsvLine(lines[1]);

            if (values.Length < 6)
            {
                return new DefenderInfo
                {
                    Available = false,
                    ErrorMessage =
                        "Die Defender-Daten konnten nicht " +
                        "ausgewertet werden."
                };
            }

            bool realTimeProtection =
                ParseBoolean(values[0]);

            bool antivirusEnabled =
                ParseBoolean(values[1]);

            bool antispywareEnabled =
                ParseBoolean(values[2]);

            string signatureVersion =
                Unquote(values[3]);

            string quickScan =
                Unquote(values[4]);

            string fullScan =
                Unquote(values[5]);

            string lastScan =
                GetLatestScan(
                    quickScan,
                    fullScan);

            return new DefenderInfo
            {
                Available = true,

                RealTimeProtection =
                    realTimeProtection,

                AntivirusEnabled =
                    antivirusEnabled,

                AntispywareEnabled =
                    antispywareEnabled,

                SignatureVersion =
                    string.IsNullOrWhiteSpace(signatureVersion)
                        ? "Unbekannt"
                        : signatureVersion,

                LastScan =
                    string.IsNullOrWhiteSpace(lastScan)
                        ? "Kein Scanzeitpunkt verfügbar"
                        : FormatDateTime(lastScan)
            };
        }
        catch (Exception ex)
        {
            return new DefenderInfo
            {
                Available = false,
                ErrorMessage =
                    $"Defender konnte nicht abgefragt werden: " +
                    $"{ex.Message}"
            };
        }
    }

    // ================================================================
    // POWERSHELL
    // ================================================================

    private async Task<string> RunPowerShellAsync(
        string script)
    {
        ProcessStartInfo startInfo =
            new ProcessStartInfo
            {
                FileName = "powershell.exe",

                Arguments =
                    $"-NoProfile -NonInteractive -Command \"{script}\"",

                UseShellExecute = false,

                CreateNoWindow = true,

                RedirectStandardOutput = true,

                RedirectStandardError = true,

                StandardOutputEncoding =
                    Encoding.UTF8
            };

        using Process process =
            new Process
            {
                StartInfo = startInfo
            };

        process.Start();

        string output =
            await process.StandardOutput.ReadToEndAsync();

        await process.WaitForExitAsync();

        return output;
    }

    // ================================================================
    // WINDOWS-FIREWALL
    // ================================================================

    private async Task<FirewallInfo> GetFirewallInformationAsync()
    {
        try
        {
            string script =
                "$profiles = Get-NetFirewallProfile; " +
                "$domain = $profiles | " +
                "Where-Object Name -eq 'Domain'; " +
                "$private = $profiles | " +
                "Where-Object Name -eq 'Private'; " +
                "$public = $profiles | " +
                "Where-Object Name -eq 'Public'; " +
                "[PSCustomObject]@{ " +
                "Domain = [bool]$domain.Enabled; " +
                "Private = [bool]$private.Enabled; " +
                "Public = [bool]$public.Enabled " +
                "} | ConvertTo-Json -Compress";

            string output =
                await RunPowerShellAsync(script);

            if (string.IsNullOrWhiteSpace(output))
            {
                return new FirewallInfo
                {
                    Available = false,
                    ErrorMessage =
                        "Windows Firewall lieferte keine Daten."
                };
            }

            using System.Text.Json.JsonDocument document =
                System.Text.Json.JsonDocument.Parse(output);

            System.Text.Json.JsonElement root =
                document.RootElement;

            if (root.ValueKind !=
                System.Text.Json.JsonValueKind.Object)
            {
                return new FirewallInfo
                {
                    Available = false,
                    ErrorMessage =
                        "Das Format der Firewall-Daten " +
                        "ist unbekannt."
                };
            }

            bool domainEnabled =
                root.TryGetProperty(
                    "Domain",
                    out System.Text.Json.JsonElement domainElement) &&
                domainElement.GetBoolean();

            bool privateEnabled =
                root.TryGetProperty(
                    "Private",
                    out System.Text.Json.JsonElement privateElement) &&
                privateElement.GetBoolean();

            bool publicEnabled =
                root.TryGetProperty(
                    "Public",
                    out System.Text.Json.JsonElement publicElement) &&
                publicElement.GetBoolean();

            return new FirewallInfo
            {
                Available = true,

                DomainEnabled =
                    domainEnabled,

                PrivateEnabled =
                    privateEnabled,

                PublicEnabled =
                    publicEnabled
            };
        }
        catch (Exception ex)
        {
            return new FirewallInfo
            {
                Available = false,
                ErrorMessage =
                    $"Firewallinformationen konnten nicht gelesen werden: " +
                    $"{ex.Message}"
            };
        }
    }

    // ================================================================
    // WINDOWS-SICHERHEIT
    // ================================================================

    private SystemSecurityInfo GetSystemSecurityInformation()
    {
        return new SystemSecurityInfo
        {
            UacStatus =
                GetUacStatus(),

            SecureBootStatus =
                GetSecureBootStatus(),

            TpmStatus =
                GetTpmStatus()
        };
    }

    // ================================================================
    // UAC
    // ================================================================

    private string GetUacStatus()
    {
        try
        {
            using RegistryKey? key =
                Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");

            if (key == null)
                return "Unbekannt";

            object? value =
                key.GetValue("EnableLUA");

            if (value is int intValue)
            {
                return intValue != 0
                    ? "Aktiviert"
                    : "Deaktiviert";
            }

            return "Unbekannt";
        }
        catch
        {
            return "Nicht verfügbar";
        }
    }

    // ================================================================
    // SECURE BOOT
    // ================================================================

    private string GetSecureBootStatus()
    {
        try
        {
            using RegistryKey? key =
                Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\SecureBoot\State");

            if (key == null)
                return "Nicht verfügbar";

            object? value =
                key.GetValue("UEFISecureBootEnabled");

            if (value is int intValue)
            {
                return intValue != 0
                    ? "Aktiviert"
                    : "Deaktiviert";
            }

            return "Unbekannt";
        }
        catch
        {
            return "Nicht verfügbar";
        }
    }

    // ================================================================
    // TPM
    // ================================================================

    private string GetTpmStatus()
    {
        try
        {
            using ManagementObjectSearcher searcher =
                new ManagementObjectSearcher(
                    @"root\CIMV2\Security\MicrosoftTpm",
                    "SELECT IsEnabled_InitialValue, " +
                    "IsActivated_InitialValue, " +
                    "IsOwned_InitialValue " +
                    "FROM Win32_Tpm");

            using ManagementObjectCollection results =
                searcher.Get();

            foreach (ManagementObject tpm in results)
            {
                bool enabled =
                    Convert.ToBoolean(
                        tpm["IsEnabled_InitialValue"] ??
                        false);

                bool activated =
                    Convert.ToBoolean(
                        tpm["IsActivated_InitialValue"] ??
                        false);

                if (enabled && activated)
                    return "Vorhanden und aktiviert";

                if (enabled)
                    return "Vorhanden, aber nicht vollständig aktiviert";

                return "Vorhanden";
            }

            return "Nicht vorhanden";
        }
        catch
        {
            return "Nicht verfügbar";
        }
    }

    // ================================================================
    // SCAN-ZEITPUNKT
    // ================================================================

    private string GetLatestScan(
        string quickScan,
        string fullScan)
    {
        DateTime quick =
            ParseDateTime(quickScan);

        DateTime full =
            ParseDateTime(fullScan);

        if (quick == DateTime.MinValue &&
            full == DateTime.MinValue)
        {
            return string.Empty;
        }

        if (quick >= full)
            return quick.ToString("O");

        return full.ToString("O");
    }

    private DateTime ParseDateTime(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DateTime.MinValue;

        if (DateTime.TryParse(
            value,
            out DateTime result))
        {
            return result;
        }

        return DateTime.MinValue;
    }

    private string FormatDateTime(
        string value)
    {
        DateTime dateTime =
            ParseDateTime(value);

        if (dateTime == DateTime.MinValue)
            return value;

        return dateTime
            .ToLocalTime()
            .ToString("dd.MM.yyyy HH:mm:ss");
    }

    // ================================================================
    // HILFSMETHODEN
    // ================================================================

    private bool ParseBoolean(
        string value)
    {
        return bool.TryParse(
            Unquote(value),
            out bool result) &&
            result;
    }

    private string Unquote(
        string value)
    {
        if (value == null)
            return string.Empty;

        string result =
            value.Trim();

        if (result.Length >= 2 &&
            result.StartsWith("\"") &&
            result.EndsWith("\""))
        {
            result =
                result.Substring(
                    1,
                    result.Length - 2);
        }

        return result.Replace(
            "\"\"",
            "\"");
    }

    private string[] ParseCsvLine(
        string line)
    {
        List<string> values =
            new List<string>();

        StringBuilder current =
            new StringBuilder();

        bool insideQuotes = false;

        foreach (char character in line)
        {
            if (character == '"')
            {
                insideQuotes =
                    !insideQuotes;

                current.Append(
                    character);

                continue;
            }

            if (character == ',' &&
                !insideQuotes)
            {
                values.Add(
                    current.ToString());

                current.Clear();

                continue;
            }

            current.Append(
                character);
        }

        values.Add(
            current.ToString());

        return values.ToArray();
    }

    // ================================================================
    // DATENMODELLE
    // ================================================================

    private class DefenderInfo
    {
        public bool Available { get; set; }

        public bool RealTimeProtection { get; set; }

        public bool AntivirusEnabled { get; set; }

        public bool AntispywareEnabled { get; set; }

        public string SignatureVersion { get; set; }
            = string.Empty;

        public string LastScan { get; set; }
            = string.Empty;

        public string? ErrorMessage { get; set; }
    }

    private class FirewallInfo
    {
        public bool Available { get; set; }

        public bool DomainEnabled { get; set; }

        public bool PrivateEnabled { get; set; }

        public bool PublicEnabled { get; set; }

        public string? ErrorMessage { get; set; }
    }

    private class SystemSecurityInfo
    {
        public string UacStatus { get; set; }
            = "Unbekannt";

        public string SecureBootStatus { get; set; }
            = "Unbekannt";

        public string TpmStatus { get; set; }
            = "Unbekannt";

        public string GetOverallStatus()
        {
            if (UacStatus == "Aktiviert" &&
                SecureBootStatus == "Aktiviert" &&
                TpmStatus.StartsWith(
                    "Vorhanden",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Grundschutz aktiv";
            }

            return "Details prüfen";
        }
    }
}