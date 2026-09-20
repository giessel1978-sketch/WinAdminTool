using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WinAdminTool.Views;

public sealed partial class AutostartPage : Page
{
    private readonly List<AutostartEntry> _entries = new();
    private readonly List<AutostartEntry> _allEntries = new();

    private const string DisabledAutostartRoot =
        @"Software\WinAdminTool\DisabledAutostart";

    private const string DisabledStartupFolderName =
        "WinAdminToolDisabled";


    public AutostartPage()
    {
        this.InitializeComponent();

        LoadAutostartEntries();
    }


    private void LoadAutostartEntries()
    {
        _entries.Clear();
        _allEntries.Clear();

        // Aktueller Benutzer
        ReadRegistryEntries(
            Registry.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "Registrierung",
            "Aktueller Benutzer");

        ReadRegistryEntries(
            Registry.CurrentUser,
            @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
            "Registrierung (RunOnce)",
            "Aktueller Benutzer");

        // Alle Benutzer
        ReadRegistryEntries(
            Registry.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "Registrierung",
            "Alle Benutzer");

        ReadRegistryEntries(
            Registry.LocalMachine,
            @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
            "Registrierung (RunOnce)",
            "Alle Benutzer");

        // Deaktivierte Registry-Einträge
        ReadDisabledRegistryEntries(
            Registry.CurrentUser,
            "HKCU_Run",
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "Registrierung",
            "Aktueller Benutzer");

        ReadDisabledRegistryEntries(
            Registry.CurrentUser,
            "HKCU_RunOnce",
            @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
            "Registrierung (RunOnce)",
            "Aktueller Benutzer");

        ReadDisabledRegistryEntries(
            Registry.LocalMachine,
            "HKLM_Run",
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            "Registrierung",
            "Alle Benutzer");

        ReadDisabledRegistryEntries(
            Registry.LocalMachine,
            "HKLM_RunOnce",
            @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
            "Registrierung (RunOnce)",
            "Alle Benutzer");

        // Aktive Autostart-Ordner
        AddStartupFolder(
            Environment.GetFolderPath(
                Environment.SpecialFolder.Startup),
            "Autostartordner",
            "Aktueller Benutzer");

        AddStartupFolder(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonStartup),
            "Autostartordner",
            "Alle Benutzer");

        // Deaktivierte Autostart-Ordner
        AddDisabledStartupFolder(
            Environment.GetFolderPath(
                Environment.SpecialFolder.Startup),
            "Autostartordner",
            "Aktueller Benutzer");

        AddDisabledStartupFolder(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonStartup),
            "Autostartordner",
            "Alle Benutzer");

        _allEntries.AddRange(_entries);

        ApplyFilters();
    }


    private void ApplyFilters()
    {
        if (SearchTextBox == null ||
            StatusFilterComboBox == null ||
            EntriesListView == null ||
            EntryCountTextBlock == null)
        {
            return;
        }

        string searchText =
            SearchTextBox.Text.Trim();

        string selectedStatus =
            (StatusFilterComboBox.SelectedItem as ComboBoxItem)?
            .Content?
            .ToString() ?? "Alle";

        IEnumerable<AutostartEntry> filteredEntries =
            _allEntries;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filteredEntries = filteredEntries.Where(entry =>
                entry.Name.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase) ||
                entry.Source.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase) ||
                entry.User.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase) ||
                entry.Command.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (selectedStatus != "Alle")
        {
            filteredEntries = filteredEntries.Where(entry =>
                entry.Status.Equals(
                    selectedStatus,
                    StringComparison.OrdinalIgnoreCase));
        }

        List<AutostartEntry> result =
            filteredEntries.ToList();

        EntriesListView.ItemsSource = result;

        EntryCountTextBlock.Text =
            $"{result.Count} von {_allEntries.Count} Einträgen";
    }


    private void SearchTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (SearchTextBox == null ||
            StatusFilterComboBox == null)
        {
            return;
        }

        ApplyFilters();
    }


    private void StatusFilterComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (SearchTextBox == null ||
            StatusFilterComboBox == null)
        {
            return;
        }

        ApplyFilters();
    }


    private void EntriesListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (EntriesListView.SelectedItem is not AutostartEntry entry)
        {
            ClearDetails();
            return;
        }

        DetailNameTextBlock.Text = entry.Name;
        DetailStatusTextBlock.Text = entry.Status;
        DetailSourceTextBlock.Text = entry.Source;
        DetailUserTextBlock.Text = entry.User;
        DetailCommandTextBlock.Text = entry.Command;

        ToggleAutostartButton.IsEnabled = true;
        OpenAutostartLocationButton.IsEnabled = true;
        RemoveAutostartButton.IsEnabled = true;

        ToggleAutostartButton.Content =
            entry.Status == "Aktiviert"
                ? "Deaktivieren"
                : "Aktivieren";
    }


    private void ClearDetails()
    {
        DetailNameTextBlock.Text =
            "Kein Eintrag ausgewählt";

        DetailStatusTextBlock.Text = "-";
        DetailSourceTextBlock.Text = "-";
        DetailUserTextBlock.Text = "-";
        DetailCommandTextBlock.Text = "-";

        ToggleAutostartButton.IsEnabled = false;
        OpenAutostartLocationButton.IsEnabled = false;
        RemoveAutostartButton.IsEnabled = false;
    }


    private void ReadRegistryEntries(
        RegistryKey rootKey,
        string subKeyPath,
        string source,
        string user)
    {
        try
        {
            using RegistryKey? key =
                rootKey.OpenSubKey(subKeyPath);

            if (key == null)
                return;

            foreach (string valueName in key.GetValueNames())
            {
                object? value = key.GetValue(
                    valueName,
                    null,
                    RegistryValueOptions.DoNotExpandEnvironmentNames);

                if (value == null)
                    continue;

                string command =
                    value.ToString() ?? string.Empty;

                RegistryValueKind valueKind =
                    key.GetValueKind(valueName);

                _entries.Add(new AutostartEntry
                {
                    Name = valueName,
                    Status = "Aktiviert",
                    Source = source,
                    User = user,
                    Command = command,
                    IsRegistryEntry = true,
                    RegistryRoot =
                        rootKey == Registry.LocalMachine
                            ? "HKLM"
                            : "HKCU",
                    RegistrySubKeyPath = subKeyPath,
                    RegistryValueKind = valueKind
                });
            }
        }
        catch
        {
            // Nicht lesbare Registry-Bereiche werden übersprungen.
        }
    }


    private void ReadDisabledRegistryEntries(
        RegistryKey rootKey,
        string disabledSubKeyName,
        string originalSubKeyPath,
        string source,
        string user)
    {
        try
        {
            string disabledPath =
                $@"{DisabledAutostartRoot}\{disabledSubKeyName}";

            using RegistryKey? key =
                rootKey.OpenSubKey(disabledPath);

            if (key == null)
                return;

            foreach (string valueName in key.GetValueNames())
            {
                object? value = key.GetValue(
                    valueName,
                    null,
                    RegistryValueOptions.DoNotExpandEnvironmentNames);

                if (value == null)
                    continue;

                string command =
                    value.ToString() ?? string.Empty;

                RegistryValueKind valueKind =
                    key.GetValueKind(valueName);

                _entries.Add(new AutostartEntry
                {
                    Name = valueName,
                    Status = "Deaktiviert",
                    Source = source,
                    User = user,
                    Command = command,
                    IsRegistryEntry = true,
                    IsDisabled = true,
                    RegistryRoot =
                        rootKey == Registry.LocalMachine
                            ? "HKLM"
                            : "HKCU",
                    RegistrySubKeyPath = originalSubKeyPath,
                    DisabledRegistrySubKeyPath = disabledPath,
                    RegistryValueKind = valueKind
                });
            }
        }
        catch
        {
            // Nicht lesbare Bereiche werden übersprungen.
        }
    }


    private void AddStartupFolder(
        string folderPath,
        string source,
        string user)
    {
        try
        {
            if (!Directory.Exists(folderPath))
                return;

            foreach (string file in Directory.GetFiles(folderPath))
            {
                _entries.Add(new AutostartEntry
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Status = "Aktiviert",
                    Source = source,
                    User = user,
                    Command = file,
                    IsRegistryEntry = false,
                    IsDisabled = false,
                    StartupFolderPath = folderPath,
                    OriginalFilePath = file
                });
            }
        }
        catch
        {
            // Nicht lesbare Ordner werden übersprungen.
        }
    }


    private void AddDisabledStartupFolder(
        string folderPath,
        string source,
        string user)
    {
        try
        {
            if (!Directory.Exists(folderPath))
                return;

            string disabledFolder =
                Path.Combine(
                    folderPath,
                    DisabledStartupFolderName);

            if (!Directory.Exists(disabledFolder))
                return;

            foreach (string file in Directory.GetFiles(disabledFolder))
            {
                _entries.Add(new AutostartEntry
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Status = "Deaktiviert",
                    Source = source,
                    User = user,
                    Command = file,
                    IsRegistryEntry = false,
                    IsDisabled = true,
                    StartupFolderPath = folderPath,
                    DisabledFolderPath = disabledFolder,
                    OriginalFilePath =
                        Path.Combine(
                            folderPath,
                            Path.GetFileName(file)),
                    DisabledFilePath = file
                });
            }
        }
        catch
        {
            // Nicht lesbare Ordner werden übersprungen.
        }
    }


    private async void ToggleAutostartButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (EntriesListView.SelectedItem is not AutostartEntry entry)
            return;

        bool success;

        if (entry.IsRegistryEntry)
        {
            success = entry.IsDisabled
                ? EnableRegistryEntry(entry)
                : DisableRegistryEntry(entry);
        }
        else
        {
            success = entry.IsDisabled
                ? EnableStartupFile(entry)
                : DisableStartupFile(entry);
        }

        if (!success)
        {
            await ShowMessageAsync(
                "Autostart",
                "Der Autostart-Eintrag konnte nicht geändert werden.");

            return;
        }

        LoadAutostartEntries();
    }


    private bool DisableRegistryEntry(
        AutostartEntry entry)
    {
        try
        {
            RegistryKey rootKey =
                GetRegistryRoot(entry.RegistryRoot);

            using RegistryKey? sourceKey =
                rootKey.OpenSubKey(
                    entry.RegistrySubKeyPath,
                    writable: true);

            if (sourceKey == null)
                return false;

            object? value = sourceKey.GetValue(
                entry.Name,
                null,
                RegistryValueOptions.DoNotExpandEnvironmentNames);

            if (value == null)
                return false;

            RegistryValueKind valueKind =
                sourceKey.GetValueKind(entry.Name);

            string disabledSubKeyName =
                GetDisabledSubKeyName(entry);

            string disabledPath =
                $@"{DisabledAutostartRoot}\{disabledSubKeyName}";

            using RegistryKey disabledKey =
                rootKey.CreateSubKey(
                    disabledPath,
                    writable: true);

            if (disabledKey == null)
                return false;

            disabledKey.SetValue(
                entry.Name,
                value,
                valueKind);

            sourceKey.DeleteValue(
                entry.Name,
                throwOnMissingValue: false);

            return true;
        }
        catch
        {
            return false;
        }
    }


    private bool EnableRegistryEntry(
        AutostartEntry entry)
    {
        try
        {
            RegistryKey rootKey =
                GetRegistryRoot(entry.RegistryRoot);

            if (string.IsNullOrWhiteSpace(
                entry.DisabledRegistrySubKeyPath))
            {
                return false;
            }

            using RegistryKey? disabledKey =
                rootKey.OpenSubKey(
                    entry.DisabledRegistrySubKeyPath,
                    writable: true);

            if (disabledKey == null)
                return false;

            object? value = disabledKey.GetValue(
                entry.Name,
                null,
                RegistryValueOptions.DoNotExpandEnvironmentNames);

            if (value == null)
                return false;

            using RegistryKey? targetKey =
                rootKey.OpenSubKey(
                    entry.RegistrySubKeyPath,
                    writable: true);

            if (targetKey == null)
                return false;

            if (targetKey.GetValue(
                entry.Name,
                null,
                RegistryValueOptions.DoNotExpandEnvironmentNames) != null)
            {
                return false;
            }

            targetKey.SetValue(
                entry.Name,
                value,
                entry.RegistryValueKind);

            disabledKey.DeleteValue(
                entry.Name,
                throwOnMissingValue: false);

            return true;
        }
        catch
        {
            return false;
        }
    }


    private string GetDisabledSubKeyName(
        AutostartEntry entry)
    {
        string prefix =
            entry.RegistryRoot == "HKLM"
                ? "HKLM"
                : "HKCU";

        if (entry.RegistrySubKeyPath.EndsWith(
            @"\RunOnce",
            StringComparison.OrdinalIgnoreCase))
        {
            return $"{prefix}_RunOnce";
        }

        return $"{prefix}_Run";
    }


    private RegistryKey GetRegistryRoot(
        string registryRoot)
    {
        return registryRoot.Equals(
            "HKLM",
            StringComparison.OrdinalIgnoreCase)
            ? Registry.LocalMachine
            : Registry.CurrentUser;
    }


    private bool DisableStartupFile(
        AutostartEntry entry)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                entry.OriginalFilePath))
            {
                return false;
            }

            if (!File.Exists(entry.OriginalFilePath))
                return false;

            string? startupFolder =
                entry.StartupFolderPath;

            if (string.IsNullOrWhiteSpace(startupFolder))
                return false;

            string disabledFolder =
                Path.Combine(
                    startupFolder,
                    DisabledStartupFolderName);

            Directory.CreateDirectory(disabledFolder);

            string disabledFile =
                Path.Combine(
                    disabledFolder,
                    Path.GetFileName(
                        entry.OriginalFilePath));

            if (File.Exists(disabledFile))
                return false;

            File.Move(
                entry.OriginalFilePath,
                disabledFile);

            return true;
        }
        catch
        {
            return false;
        }
    }


    private bool EnableStartupFile(
        AutostartEntry entry)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(
                entry.DisabledFilePath) ||
                string.IsNullOrWhiteSpace(
                entry.OriginalFilePath))
            {
                return false;
            }

            if (!File.Exists(entry.DisabledFilePath))
                return false;

            if (File.Exists(entry.OriginalFilePath))
                return false;

            File.Move(
                entry.DisabledFilePath,
                entry.OriginalFilePath);

            return true;
        }
        catch
        {
            return false;
        }
    }


    private async void RemoveAutostartButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (EntriesListView.SelectedItem is not AutostartEntry entry)
            return;

        ContentDialog dialog = new ContentDialog
        {
            Title = "Autostart-Eintrag entfernen?",
            Content =
                $"Der Eintrag „{entry.Name}“ wird dauerhaft aus dem Autostart entfernt.\n\n" +
                "Diese Aktion kann nicht rückgängig gemacht werden.",
            PrimaryButtonText = "Entfernen",
            CloseButtonText = "Abbrechen",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        ContentDialogResult result =
            await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        bool success;

        if (entry.IsRegistryEntry)
        {
            success = RemoveRegistryEntry(entry);
        }
        else
        {
            success = RemoveStartupFile(entry);
        }

        if (!success)
        {
            await ShowMessageAsync(
                "Autostart",
                "Der Eintrag konnte nicht entfernt werden.");

            return;
        }

        LoadAutostartEntries();
    }


    private bool RemoveRegistryEntry(
        AutostartEntry entry)
    {
        try
        {
            RegistryKey rootKey =
                GetRegistryRoot(entry.RegistryRoot);

            if (entry.IsDisabled)
            {
                if (string.IsNullOrWhiteSpace(
                    entry.DisabledRegistrySubKeyPath))
                {
                    return false;
                }

                using RegistryKey? disabledKey =
                    rootKey.OpenSubKey(
                        entry.DisabledRegistrySubKeyPath,
                        writable: true);

                if (disabledKey == null)
                    return false;

                disabledKey.DeleteValue(
                    entry.Name,
                    throwOnMissingValue: false);

                return true;
            }

            using RegistryKey? key =
                rootKey.OpenSubKey(
                    entry.RegistrySubKeyPath,
                    writable: true);

            if (key == null)
                return false;

            key.DeleteValue(
                entry.Name,
                throwOnMissingValue: false);

            return true;
        }
        catch
        {
            return false;
        }
    }


    private bool RemoveStartupFile(
        AutostartEntry entry)
    {
        try
        {
            string filePath =
                entry.IsDisabled
                    ? entry.DisabledFilePath
                    : entry.OriginalFilePath;

            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            if (!File.Exists(filePath))
                return false;

            File.Delete(filePath);

            return true;
        }
        catch
        {
            return false;
        }
    }


    private async void OpenAutostartLocationButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (EntriesListView.SelectedItem is not AutostartEntry entry)
            return;

        try
        {
            if (!entry.IsRegistryEntry)
            {
                string filePath =
                    entry.IsDisabled &&
                    !string.IsNullOrWhiteSpace(
                        entry.DisabledFilePath)
                        ? entry.DisabledFilePath
                        : entry.OriginalFilePath;

                if (!string.IsNullOrWhiteSpace(filePath) &&
                    File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments =
                            $"/select,\"{filePath}\"",
                        UseShellExecute = true
                    });
                }

                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "regedit.exe",
                UseShellExecute = true
            });
        }
        catch
        {
            await ShowMessageAsync(
                "Speicherort",
                "Der Speicherort konnte nicht geöffnet werden.");
        }
    }


    private async System.Threading.Tasks.Task ShowMessageAsync(
        string title,
        string message)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }
}


public class AutostartEntry
{
    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string User { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;

    public bool IsRegistryEntry { get; set; }

    public bool IsDisabled { get; set; }

    public string RegistryRoot { get; set; } = string.Empty;

    public string RegistrySubKeyPath { get; set; } = string.Empty;

    public string DisabledRegistrySubKeyPath { get; set; } = string.Empty;

    public RegistryValueKind RegistryValueKind { get; set; } =
        RegistryValueKind.String;

    public string StartupFolderPath { get; set; } = string.Empty;

    public string OriginalFilePath { get; set; } = string.Empty;

    public string DisabledFolderPath { get; set; } = string.Empty;

    public string DisabledFilePath { get; set; } = string.Empty;
}