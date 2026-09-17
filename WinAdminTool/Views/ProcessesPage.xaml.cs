using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

        protected override void OnNavigatedFrom(
            Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            _refreshTimer.Stop();

            base.OnNavigatedFrom(e);
        }
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