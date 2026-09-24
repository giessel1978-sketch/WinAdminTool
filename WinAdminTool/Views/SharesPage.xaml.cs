using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace WinAdminTool.Views;

public sealed partial class SharesPage : Page
{
    private List<ShareInfo> _shares = new();
    private List<SessionInfo> _sessions = new();
    private List<OpenFileInfo> _openFiles = new();

    public SharesPage()
    {
        InitializeComponent();

        Loaded += SharesPage_Loaded;
    }

    private async void SharesPage_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAllAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAllAsync();
    }

    private async Task RefreshAllAsync()
    {
        HideAllDetails();

        await LoadSharesAsync();
        await LoadSessionsAsync();
        await LoadOpenFilesAsync();

        ShowSharesView();
    }

    private void ShareCardButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSharesView();
    }

    private void SessionCardButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSessionsView();
    }

    private void OpenFileCardButton_Click(object sender, RoutedEventArgs e)
    {
        ShowOpenFilesView();
    }

    private void ShowSharesView()
    {
        CurrentSectionText.Text = "Freigaben";

        SharesListView.ItemsSource = _shares;

        SharesListView.SelectionChanged -=
            SharesListView_SelectionChanged;

        SharesListView.SelectionChanged +=
            SharesListView_SelectionChanged;

        HideAllDetails();
        DetailTitleText.Text = "Details";

        if (_shares.Count == 0)
        {
            ShowHint("Keine SMB-Freigaben vorhanden.");
        }
        else
        {
            ShowHint("Bitte eine Freigabe auswählen.");
        }
    }

    private void ShowSessionsView()
    {
        CurrentSectionText.Text = "Aktive Sitzungen";

        SharesListView.SelectionChanged -=
            SharesListView_SelectionChanged;

        SharesListView.ItemsSource = _sessions;

        HideAllDetails();
        DetailTitleText.Text = "Details";

        if (_sessions.Count == 0)
        {
            ShowHint("Aktuell sind keine aktiven SMB-Sitzungen vorhanden.");
            return;
        }

        ShowHint("Bitte eine Sitzung auswählen.");

        SharesListView.SelectionChanged +=
            SessionsListView_SelectionChanged;
    }

    private void ShowOpenFilesView()
    {
        CurrentSectionText.Text = "Geöffnete Dateien";

        SharesListView.SelectionChanged -=
            SharesListView_SelectionChanged;

        SharesListView.SelectionChanged -=
            SessionsListView_SelectionChanged;

        SharesListView.ItemsSource = _openFiles;

        HideAllDetails();
        DetailTitleText.Text = "Details";

        if (_openFiles.Count == 0)
        {
            ShowHint("Aktuell sind keine über SMB geöffneten Dateien vorhanden.");
            return;
        }

        ShowHint("Bitte eine geöffnete Datei auswählen.");

        SharesListView.SelectionChanged +=
            OpenFilesListView_SelectionChanged;
    }

    private void SharesListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (SharesListView.SelectedItem is ShareInfo share)
        {
            HideAllDetails();

            DetailTitleText.Text = "Freigabe";

            DetailHintText.Visibility = Visibility.Collapsed;
            ShareDetailsPanel.Visibility = Visibility.Visible;

            DetailNameText.Text = share.Name ?? "-";
            DetailPathText.Text = share.Path ?? "-";
            DetailDescriptionText.Text =
                string.IsNullOrWhiteSpace(share.Description)
                    ? "-"
                    : share.Description;
        }
    }

    private void SessionsListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (SharesListView.SelectedItem is SessionInfo session)
        {
            HideAllDetails();

            DetailTitleText.Text = "Aktive Sitzung";

            DetailHintText.Visibility = Visibility.Collapsed;
            SessionDetailsPanel.Visibility = Visibility.Visible;

            DetailClientComputerText.Text =
                string.IsNullOrWhiteSpace(session.ClientComputerName)
                    ? "-"
                    : session.ClientComputerName;

            DetailClientUserText.Text =
                string.IsNullOrWhiteSpace(session.ClientUserName)
                    ? "-"
                    : session.ClientUserName;

            DetailSessionIdText.Text =
                session.SessionId.ToString();

            DetailNumOpensText.Text =
                session.NumOpens.ToString();
        }
    }

    private void OpenFilesListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (SharesListView.SelectedItem is OpenFileInfo openFile)
        {
            HideAllDetails();

            DetailTitleText.Text = "Geöffnete Datei";

            DetailHintText.Visibility = Visibility.Collapsed;
            OpenFileDetailsPanel.Visibility = Visibility.Visible;

            DetailFilePathText.Text =
                string.IsNullOrWhiteSpace(openFile.Path)
                    ? "-"
                    : openFile.Path;

            DetailFileClientText.Text =
                string.IsNullOrWhiteSpace(openFile.ClientComputerName)
                    ? "-"
                    : openFile.ClientComputerName;

            DetailFileUserText.Text =
                string.IsNullOrWhiteSpace(openFile.ClientUserName)
                    ? "-"
                    : openFile.ClientUserName;

            DetailFileSessionIdText.Text =
                openFile.SessionId.ToString();

            DetailFileIdText.Text =
                openFile.FileId.ToString();
        }
    }

    private async Task LoadSharesAsync()
    {
        StatusText.Text = "Lade Freigaben...";
        SharesListView.ItemsSource = null;

        try
        {
            string script =
                "Get-SmbShare | " +
                "Select-Object Name,Path,Description | " +
                "ConvertTo-Json -Compress";

            var result =
                await RunPowerShellAsync(script);

            if (result.ExitCode != 0)
            {
                StatusText.Text =
                    string.IsNullOrWhiteSpace(result.Error)
                        ? "Die Freigaben konnten nicht ermittelt werden."
                        : result.Error.Trim();

                return;
            }

            _shares =
                DeserializeList<ShareInfo>(result.Output);

            ShareCountText.Text =
                _shares.Count.ToString();
        }
        catch (Exception ex)
        {
            StatusText.Text =
                $"Fehler beim Ermitteln der Freigaben: {ex.Message}";
        }
    }

    private async Task LoadSessionsAsync()
    {
        try
        {
            string script =
                "Get-SmbSession | " +
                "Select-Object SessionId,ClientComputerName,ClientUserName,NumOpens | " +
                "ConvertTo-Json -Compress";

            var result =
                await RunPowerShellAsync(script);

            if (result.ExitCode != 0)
            {
                _sessions = new List<SessionInfo>();
                SessionCountText.Text = "0";
                return;
            }

            _sessions =
                DeserializeList<SessionInfo>(result.Output);

            SessionCountText.Text =
                _sessions.Count.ToString();
        }
        catch
        {
            _sessions = new List<SessionInfo>();
            SessionCountText.Text = "0";
        }
    }

    private async Task LoadOpenFilesAsync()
    {
        try
        {
            string script =
                "Get-SmbOpenFile | " +
                "Select-Object FileId,SessionId,ClientComputerName,ClientUserName,Path,ShareRelativePath | " +
                "ConvertTo-Json -Compress";

            var result =
                await RunPowerShellAsync(script);

            if (result.ExitCode != 0)
            {
                _openFiles = new List<OpenFileInfo>();
                OpenFileCountText.Text = "0";
                return;
            }

            _openFiles =
                DeserializeList<OpenFileInfo>(result.Output);

            OpenFileCountText.Text =
                _openFiles.Count.ToString();
        }
        catch
        {
            _openFiles = new List<OpenFileInfo>();
            OpenFileCountText.Text = "0";
        }
    }

    private static async Task<(string Output, string Error, int ExitCode)>
        RunPowerShellAsync(string script)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments =
                $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using Process? process =
            Process.Start(startInfo);

        if (process == null)
        {
            return (
                "",
                "PowerShell konnte nicht gestartet werden.",
                -1);
        }

        string output =
            await process.StandardOutput.ReadToEndAsync();

        string error =
            await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return (
            output,
            error,
            process.ExitCode);
    }

    private static List<T> DeserializeList<T>(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return new List<T>();
        }

        using JsonDocument document =
            JsonDocument.Parse(output);

        if (document.RootElement.ValueKind ==
            JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<T>>(output)
                   ?? new List<T>();
        }

        T? singleItem =
            JsonSerializer.Deserialize<T>(output);

        return singleItem != null
            ? new List<T> { singleItem }
            : new List<T>();
    }

    private void HideAllDetails()
    {
        DetailHintText.Visibility =
            Visibility.Collapsed;

        ShareDetailsPanel.Visibility =
            Visibility.Collapsed;

        SessionDetailsPanel.Visibility =
            Visibility.Collapsed;

        OpenFileDetailsPanel.Visibility =
            Visibility.Collapsed;

        SharesListView.SelectionChanged -=
            SessionsListView_SelectionChanged;

        SharesListView.SelectionChanged -=
            OpenFilesListView_SelectionChanged;
    }

    private void ShowHint(string text)
    {
        DetailHintText.Text = text;
        DetailHintText.Visibility =
            Visibility.Visible;
    }

    public class ShareInfo
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Description { get; set; }
    }

    public class SessionInfo
    {
        public ulong SessionId { get; set; }
        public string? ClientComputerName { get; set; }
        public string? ClientUserName { get; set; }
        public uint NumOpens { get; set; }
    }

    public class OpenFileInfo
    {
        public ulong FileId { get; set; }
        public ulong SessionId { get; set; }
        public string? ClientComputerName { get; set; }
        public string? ClientUserName { get; set; }
        public string? Path { get; set; }
        public string? ShareRelativePath { get; set; }
    }
}