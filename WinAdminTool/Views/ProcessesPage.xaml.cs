using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

namespace WinAdminTool.Views
{
    public sealed partial class ProcessesPage : Page
    {
        private readonly ObservableCollection<ProcessInfo> _processes = new();

        private readonly ObservableCollection<ProcessInfo> _displayedProcesses = new();

        private readonly DispatcherTimer _refreshTimer;

        private bool _isRefreshing;

        public ProcessesPage()
        {
            InitializeComponent();

            // Die angezeigte Liste wird einmalig an die ListView gebunden.
            ProcessListView.ItemsSource = _displayedProcesses;

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            _refreshTimer.Tick += RefreshTimer_Tick;

            ClearProcessDetails();

            _ = LoadProcessesAsync();

            _refreshTimer.Start();
        }

        private async void RefreshTimer_Tick(
            object? sender,
            object e)
        {
            await LoadProcessesAsync();
        }

        private async Task LoadProcessesAsync()
        {
            if (_isRefreshing)
                return;

            _isRefreshing = true;

            try
            {
                List<ProcessInfo> newProcesses =
                    await Task.Run(() =>
                    {
                        List<ProcessInfo> result = new();

                        foreach (Process process in Process.GetProcesses())
                        {
                            try
                            {
                                result.Add(new ProcessInfo
                                {
                                    Name = process.ProcessName,
                                    Id = process.Id,
                                    MemoryUsage =
                                        process.WorkingSet64 /
                                        1024.0 /
                                        1024.0
                                });
                            }
                            catch
                            {
                                // Prozess kann während des Auslesens verschwinden.
                            }
                            finally
                            {
                                process.Dispose();
                            }
                        }

                        return result;
                    });

                MergeProcesses(newProcesses);

                ProcessCountText.Text =
                    $"{_processes.Count} Prozesse aktiv";

                await UpdateCpuUsageAsync();

                // Ausgewählten Prozess nach einer Aktualisierung
                // erneut in der Detailansicht anzeigen.
                if (ProcessListView.SelectedItem is ProcessInfo selectedProcess)
                {
                    await ShowProcessDetailsAsync(selectedProcess);
                }
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void MergeProcesses(
            List<ProcessInfo> newProcesses)
        {
            Dictionary<int, ProcessInfo> newProcessDictionary =
                newProcesses.ToDictionary(p => p.Id);

            // Bestehende Prozesse aktualisieren
            foreach (ProcessInfo existingProcess in _processes.ToList())
            {
                if (newProcessDictionary.TryGetValue(
                    existingProcess.Id,
                    out ProcessInfo? newProcess))
                {
                    existingProcess.Name = newProcess.Name;

                    existingProcess.MemoryUsage =
                        newProcess.MemoryUsage;
                }
                else
                {
                    // Prozess existiert nicht mehr.
                    _processes.Remove(existingProcess);

                    _displayedProcesses.Remove(existingProcess);

                    // Falls der gerade ausgewählte Prozess
                    // beendet wurde, Details zurücksetzen.
                    if (ProcessListView.SelectedItem == existingProcess)
                    {
                        ProcessListView.SelectedItem = null;
                        ClearProcessDetails();
                    }
                }
            }

            // Neue Prozesse hinzufügen
            HashSet<int> existingIds =
                _processes
                    .Select(p => p.Id)
                    .ToHashSet();

            foreach (ProcessInfo newProcess in newProcesses)
            {
                if (!existingIds.Contains(newProcess.Id))
                {
                    _processes.Add(newProcess);

                    if (MatchesSearch(newProcess))
                    {
                        _displayedProcesses.Add(newProcess);
                    }
                }
            }
        }

        private async Task UpdateCpuUsageAsync()
        {
            List<ProcessInfo> processSnapshot =
                _processes.ToList();

            Dictionary<int, TimeSpan> startCpuTimes = new();

            // Ersten CPU-Zeitpunkt aller Prozesse erfassen
            await Task.Run(() =>
            {
                foreach (ProcessInfo processInfo in processSnapshot)
                {
                    try
                    {
                        using Process process =
                            Process.GetProcessById(processInfo.Id);

                        startCpuTimes[processInfo.Id] =
                            process.TotalProcessorTime;
                    }
                    catch
                    {
                        // Prozess wurde beendet oder ist nicht mehr verfügbar.
                    }
                }
            });

            // Kurzes Messintervall
            await Task.Delay(500);

            // Zweiten CPU-Zeitpunkt erfassen
            await Task.Run(() =>
            {
                foreach (ProcessInfo processInfo in processSnapshot)
                {
                    try
                    {
                        using Process process =
                            Process.GetProcessById(processInfo.Id);

                        if (!startCpuTimes.TryGetValue(
                            processInfo.Id,
                            out TimeSpan startCpu))
                        {
                            continue;
                        }

                        TimeSpan endCpu =
                            process.TotalProcessorTime;

                        double cpuMilliseconds =
                            (endCpu - startCpu).TotalMilliseconds;

                        double cpuUsage =
                            cpuMilliseconds /
                            (500.0 * Environment.ProcessorCount) *
                            100.0;

                        processInfo.CpuUsage =
                            Math.Max(0, cpuUsage);

                        processInfo.MemoryUsage =
                            process.WorkingSet64 /
                            1024.0 /
                            1024.0;
                    }
                    catch
                    {
                        // Prozess wurde während der Messung beendet.
                    }
                }
            });
        }

        private bool MatchesSearch(ProcessInfo process)
        {
            string searchText =
                ProcessSearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            return process.Name.Contains(
                searchText,
                StringComparison.OrdinalIgnoreCase);
        }

        private void ProcessSearchBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            _displayedProcesses.Clear();

            foreach (ProcessInfo process in _processes)
            {
                if (MatchesSearch(process))
                {
                    _displayedProcesses.Add(process);
                }
            }
        }

        private async void ProcessListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (ProcessListView.SelectedItem is ProcessInfo processInfo)
            {
                await ShowProcessDetailsAsync(processInfo);
            }
            else
            {
                ClearProcessDetails();
            }
        }

        private async Task ShowProcessDetailsAsync(
            ProcessInfo processInfo)
        {
            DetailProcessName.Text =
                processInfo.Name;

            DetailProcessId.Text =
                processInfo.Id.ToString();

            DetailCpu.Text =
                processInfo.CpuDisplay;

            DetailMemory.Text =
                processInfo.MemoryDisplay;

            try
            {
                ProcessDetails details =
                    await Task.Run(() =>
                        ReadProcessDetails(processInfo.Id));

                DetailStatus.Text =
                    details.Status;

                DetailPriority.Text =
                    details.Priority;

                DetailUser.Text =
                    details.User;

                DetailStartTime.Text =
                    details.StartTime;

                DetailWindowTitle.Text =
                    details.WindowTitle;

                DetailExecutablePath.Text =
                    details.ExecutablePath;

                OpenExecutableLocationButton.IsEnabled =
                    !string.IsNullOrWhiteSpace(details.ExecutablePath) &&
                    File.Exists(details.ExecutablePath);

                TerminateProcessButton.IsEnabled = true;
            }
            catch
            {
                DetailStatus.Text = "Nicht verfügbar";
                DetailPriority.Text = "Nicht verfügbar";
                DetailUser.Text = "Nicht verfügbar";
                DetailStartTime.Text = "Nicht verfügbar";
                DetailWindowTitle.Text = "Nicht verfügbar";
                DetailExecutablePath.Text = "Nicht verfügbar";

                OpenExecutableLocationButton.IsEnabled = false;
                TerminateProcessButton.IsEnabled = false;
            }
        }

        private ProcessDetails ReadProcessDetails(int processId)
        {
            ProcessDetails details = new();

            try
            {
                using Process process =
                    Process.GetProcessById(processId);

                details.Status =
                    process.HasExited
                        ? "Beendet"
                        : "Wird ausgeführt";

                try
                {
                    details.Priority =
                        process.PriorityClass.ToString();
                }
                catch
                {
                    details.Priority = "Nicht verfügbar";
                }

                try
                {
                    details.StartTime =
                        process.StartTime.ToString(
                            "dd.MM.yyyy HH:mm:ss");
                }
                catch
                {
                    details.StartTime = "Nicht verfügbar";
                }

                try
                {
                    details.WindowTitle =
                        string.IsNullOrWhiteSpace(
                            process.MainWindowTitle)
                            ? "-"
                            : process.MainWindowTitle;
                }
                catch
                {
                    details.WindowTitle = "Nicht verfügbar";
                }

                try
                {
                    details.ExecutablePath =
                        process.MainModule?.FileName
                        ?? "Nicht verfügbar";
                }
                catch
                {
                    details.ExecutablePath =
                        "Zugriff verweigert";
                }

                // Benutzerkonto des Prozesses ermitteln.
                details.User =
                    GetProcessUser(process);
            }
            catch
            {
                details.Status = "Nicht verfügbar";
                details.Priority = "Nicht verfügbar";
                details.StartTime = "Nicht verfügbar";
                details.WindowTitle = "Nicht verfügbar";
                details.ExecutablePath = "Nicht verfügbar";
                details.User = "Nicht verfügbar";
            }

            return details;
        }

        private string GetProcessUser(Process process)
        {
            try
            {
                if (!OpenProcessToken(
                    process.Handle,
                    TOKEN_QUERY,
                    out IntPtr tokenHandle))
                {
                    return "Nicht verfügbar";
                }

                try
                {
                    uint tokenInformationLength = 0;

                    GetTokenInformation(
                        tokenHandle,
                        TokenUserInformationClass,
                        IntPtr.Zero,
                        0,
                        out tokenInformationLength);

                    if (tokenInformationLength == 0)
                        return "Nicht verfügbar";

                    IntPtr tokenInformation =
                        Marshal.AllocHGlobal(
                            (int)tokenInformationLength);

                    try
                    {
                        if (!GetTokenInformation(
                            tokenHandle,
                            TokenUserInformationClass,
                            tokenInformation,
                            tokenInformationLength,
                            out _))
                        {
                            return "Nicht verfügbar";
                        }

                        TOKEN_USER tokenUser =
                            Marshal.PtrToStructure<TOKEN_USER>(
                                tokenInformation);

                        if (tokenUser.User.Sid == IntPtr.Zero)
                            return "Nicht verfügbar";

                        SecurityIdentifier sid =
                            new SecurityIdentifier(
                                tokenUser.User.Sid);

                        uint accountNameLength = 0;
                        uint domainNameLength = 0;

                        SID_NAME_USE sidType;

                        // Benötigte Puffergrößen ermitteln.
                        LookupAccountSid(
                            null,
                            tokenUser.User.Sid,
                            null,
                            ref accountNameLength,
                            null,
                            ref domainNameLength,
                            out sidType);

                        if (accountNameLength == 0)
                            return sid.Value;

                        string accountName =
                            new string(
                                '\0',
                                (int)accountNameLength);

                        string domainName =
                            new string(
                                '\0',
                                (int)domainNameLength);

                        if (!LookupAccountSid(
                            null,
                            tokenUser.User.Sid,
                            accountName,
                            ref accountNameLength,
                            domainName,
                            ref domainNameLength,
                            out sidType))
                        {
                            return sid.Value;
                        }

                        accountName =
                            accountName.TrimEnd('\0');

                        domainName =
                            domainName.TrimEnd('\0');

                        if (!string.IsNullOrWhiteSpace(domainName))
                        {
                            return
                                $"{domainName}\\{accountName}";
                        }

                        return accountName;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(
                            tokenInformation);
                    }
                }
                finally
                {
                    CloseHandle(tokenHandle);
                }
            }
            catch
            {
                return "Nicht verfügbar";
            }
        }

        private async void TerminateProcessButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (ProcessListView.SelectedItem is not ProcessInfo processInfo)
                return;

            ContentDialog dialog = new ContentDialog
            {
                Title = "Prozess beenden?",
                Content =
                    $"Soll der Prozess \"{processInfo.Name}\" " +
                    $"(PID {processInfo.Id}) wirklich beendet werden?",
                PrimaryButtonText = "Prozess beenden",
                CloseButtonText = "Abbrechen",
                DefaultButton = ContentDialogButton.Close
            };

            dialog.XamlRoot = Content.XamlRoot;

            ContentDialogResult result =
                await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            try
            {
                await Task.Run(() =>
                {
                    using Process process =
                        Process.GetProcessById(processInfo.Id);

                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                });

                ProcessListView.SelectedItem = null;

                ClearProcessDetails();

                await LoadProcessesAsync();
            }
            catch (ArgumentException)
            {
                await ShowMessageAsync(
                    "Prozess nicht gefunden",
                    "Der Prozess wurde bereits beendet.");
            }
            catch (InvalidOperationException)
            {
                await ShowMessageAsync(
                    "Prozess konnte nicht beendet werden",
                    "Der Prozess kann momentan nicht beendet werden.");
            }
            catch (Win32Exception)
            {
                await ShowMessageAsync(
                    "Zugriff verweigert",
                    "Der Prozess darf nicht beendet werden. " +
                    "Möglicherweise fehlen Administratorrechte oder " +
                    "der Prozess ist geschützt.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync(
                    "Fehler",
                    $"Der Prozess konnte nicht beendet werden.\n\n{ex.Message}");
            }
        }

        private async void OpenExecutableLocationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (ProcessListView.SelectedItem is not ProcessInfo processInfo)
                return;

            try
            {
                string? executablePath =
                    await Task.Run(() =>
                    {
                        using Process process =
                            Process.GetProcessById(processInfo.Id);

                        return process.MainModule?.FileName;
                    });

                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    await ShowMessageAsync(
                        "Speicherort nicht verfügbar",
                        "Der Speicherort der ausführbaren Datei " +
                        "konnte nicht ermittelt werden.");

                    return;
                }

                if (!File.Exists(executablePath))
                {
                    await ShowMessageAsync(
                        "Datei nicht gefunden",
                        "Die ausführbare Datei wurde nicht gefunden.");

                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{executablePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Win32Exception)
            {
                await ShowMessageAsync(
                    "Zugriff verweigert",
                    "Der Speicherort der ausführbaren Datei " +
                    "konnte nicht geöffnet werden.");
            }
            catch
            {
                await ShowMessageAsync(
                    "Speicherort nicht verfügbar",
                    "Der Speicherort der ausführbaren Datei " +
                    "konnte nicht ermittelt werden.");
            }
        }

        private async Task ShowMessageAsync(
            string title,
            string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close
            };

            dialog.XamlRoot = Content.XamlRoot;

            await dialog.ShowAsync();
        }

        private void ClearProcessDetails()
        {
            DetailProcessName.Text =
                "Kein Prozess ausgewählt";

            DetailProcessId.Text = "-";
            DetailStatus.Text = "-";
            DetailCpu.Text = "-";
            DetailMemory.Text = "-";
            DetailPriority.Text = "-";
            DetailUser.Text = "-";
            DetailStartTime.Text = "-";
            DetailWindowTitle.Text = "-";
            DetailExecutablePath.Text = "-";

            OpenExecutableLocationButton.IsEnabled = false;
            TerminateProcessButton.IsEnabled = false;
        }

        protected override void OnNavigatedFrom(
            Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            _refreshTimer.Stop();

            base.OnNavigatedFrom(e);
        }

        // ============================================================
        // Windows API
        // ============================================================

        private const uint TOKEN_QUERY = 0x0008;

        private const int TokenUserInformationClass = 1;

        private enum SID_NAME_USE
        {
            SidTypeUser = 1,
            SidTypeGroup,
            SidTypeDomain,
            SidTypeAlias,
            SidTypeWellKnownGroup,
            SidTypeDeletedAccount,
            SidTypeInvalid,
            SidTypeUnknown,
            SidTypeComputer
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_USER
        {
            public SID_AND_ATTRIBUTES User;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public uint Attributes;
        }

        [DllImport(
            "advapi32.dll",
            SetLastError = true)]
        private static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            uint DesiredAccess,
            out IntPtr TokenHandle);

        [DllImport(
            "advapi32.dll",
            SetLastError = true)]
        private static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            int TokenInformationClass,
            IntPtr TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);

        [DllImport(
            "advapi32.dll",
            SetLastError = true,
            CharSet = CharSet.Unicode)]
        private static extern bool LookupAccountSid(
            string? lpSystemName,
            IntPtr Sid,
            string? Name,
            ref uint cchName,
            string? ReferencedDomainName,
            ref uint cchReferencedDomainName,
            out SID_NAME_USE peUse);

        [DllImport(
            "kernel32.dll",
            SetLastError = true)]
        private static extern bool CloseHandle(
            IntPtr hObject);
    }

    public class ProcessDetails
    {
        public string Status { get; set; } = "-";

        public string Priority { get; set; } = "-";

        public string User { get; set; } = "-";

        public string StartTime { get; set; } = "-";

        public string WindowTitle { get; set; } = "-";

        public string ExecutablePath { get; set; } = "-";
    }

    public class ProcessInfo : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _id;
        private double _cpuUsage;
        private double _memoryUsage;

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;

                _name = value;
                OnPropertyChanged();
            }
        }

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value)
                    return;

                _id = value;
                OnPropertyChanged();
            }
        }

        public double CpuUsage
        {
            get => _cpuUsage;
            set
            {
                if (Math.Abs(_cpuUsage - value) < 0.01)
                    return;

                _cpuUsage = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(CpuDisplay));
            }
        }

        public double MemoryUsage
        {
            get => _memoryUsage;
            set
            {
                if (Math.Abs(_memoryUsage - value) < 0.01)
                    return;

                _memoryUsage = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(MemoryDisplay));
            }
        }

        public string CpuDisplay =>
            $"{CpuUsage:F1} %";

        public string MemoryDisplay =>
            $"{MemoryUsage:F1} MB";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}