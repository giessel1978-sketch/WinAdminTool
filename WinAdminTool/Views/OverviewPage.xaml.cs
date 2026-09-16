using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace WinAdminTool.Views
{
    public sealed partial class OverviewPage : Page
    {
        private CancellationTokenSource? _monitoringCancellationTokenSource;

        private ulong _previousIdleTime;
        private ulong _previousKernelTime;
        private ulong _previousUserTime;

        public OverviewPage()
        {
            this.InitializeComponent();

            LoadSystemInformation();
            LoadCpuInformation();

            StartCpuMonitoring();
            StartMemoryMonitoring();
            StartStorageMonitoring();
        }

        private void StartStorageMonitoring()
        {
            _ = MonitorStorageUsageAsync();
        }

        private async Task MonitorStorageUsageAsync()
        {
            while (_monitoringCancellationTokenSource != null &&
                   !_monitoringCancellationTokenSource.IsCancellationRequested)
            {
                UpdateStorageInformation();

                try
                {
                    await Task.Delay(
                        2000,
                        _monitoringCancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private void UpdateStorageInformation()
        {
            try
            {
                string systemDrive =
                    Path.GetPathRoot(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.System))
                    ?? "C:\\";

                DriveInfo drive = new DriveInfo(systemDrive);

                if (!drive.IsReady)
                {
                    StorageUsageText.Text = "Nicht verfügbar";
                    StorageProgressBar.Value = 0;
                    return;
                }

                long totalSpace = drive.TotalSize;
                long freeSpace = drive.AvailableFreeSpace;
                long usedSpace = totalSpace - freeSpace;

                double usage =
                    totalSpace > 0
                        ? (double)usedSpace / totalSpace * 100.0
                        : 0;

                StorageUsageText.Text =
                    $"{FormatStorageSize(usedSpace)} / " +
                    $"{FormatStorageSize(totalSpace)} " +
                    $"({usage:0} %)";

                StorageProgressBar.Value =
                    Math.Clamp(usage, 0, 100);
            }
            catch
            {
                StorageUsageText.Text = "Unbekannt";
                StorageProgressBar.Value = 0;
            }
        }

        private static string FormatStorageSize(long bytes)
        {
            const double terabyte =
                1024.0 * 1024.0 * 1024.0 * 1024.0;

            const double gigabyte =
                1024.0 * 1024.0 * 1024.0;

            if (bytes >= terabyte)
            {
                return $"{bytes / terabyte:0.0} TB";
            }

            return $"{bytes / gigabyte:0.0} GB";
        }
        private void LoadSystemInformation()
        {
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

        private void LoadCpuInformation()
        {
            try
            {
                CpuNameText.Text = GetProcessorName();

                CpuCoresText.Text =
                    GetPhysicalCoreCount().ToString();

                CpuLogicalProcessorsText.Text =
                    Environment.ProcessorCount.ToString();
            }
            catch
            {
                CpuNameText.Text = "Unbekannt";
                CpuCoresText.Text = "Unbekannt";
                CpuLogicalProcessorsText.Text = "Unbekannt";
            }
        }

        private string GetProcessorName()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");

                string? processorName =
                    key?.GetValue("ProcessorNameString") as string;

                if (!string.IsNullOrWhiteSpace(processorName))
                {
                    return processorName.Trim();
                }
            }
            catch
            {
                // Falls der Registry-Zugriff fehlschlägt.
            }

            return "Unbekannt";
        }

        private int GetPhysicalCoreCount()
        {
            uint length = 0;

            GetLogicalProcessorInformationEx(
                LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore,
                IntPtr.Zero,
                ref length);

            if (length == 0)
            {
                return Environment.ProcessorCount;
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)length);

            try
            {
                if (!GetLogicalProcessorInformationEx(
                        LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore,
                        buffer,
                        ref length))
                {
                    return Environment.ProcessorCount;
                }

                int physicalCoreCount = 0;
                int offset = 0;

                while (offset < length)
                {
                    IntPtr current =
                        IntPtr.Add(buffer, offset);

                    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX info =
                        Marshal.PtrToStructure<
                            SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(
                                current);

                    if (info.Relationship ==
                        LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore)
                    {
                        physicalCoreCount++;
                    }

                    if (info.Size == 0)
                    {
                        break;
                    }

                    offset += (int)info.Size;
                }

                return physicalCoreCount > 0
                    ? physicalCoreCount
                    : Environment.ProcessorCount;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private void StartCpuMonitoring()
        {
            _monitoringCancellationTokenSource =
                new CancellationTokenSource();

            _ = MonitorCpuUsageAsync(
                _monitoringCancellationTokenSource.Token);
        }

        private async Task MonitorCpuUsageAsync(
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                double cpuUsage = GetCpuUsage();

                CpuUsageText.Text = $"{cpuUsage:0} %";
                CpuProgressBar.Value = cpuUsage;

                try
                {
                    await Task.Delay(1000, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private double GetCpuUsage()
        {
            if (!GetSystemTimes(
                    out FILETIME idleTime,
                    out FILETIME kernelTime,
                    out FILETIME userTime))
            {
                return 0;
            }

            ulong idle = FileTimeToUInt64(idleTime);
            ulong kernel = FileTimeToUInt64(kernelTime);
            ulong user = FileTimeToUInt64(userTime);

            if (_previousKernelTime == 0)
            {
                _previousIdleTime = idle;
                _previousKernelTime = kernel;
                _previousUserTime = user;

                return 0;
            }

            ulong idleDifference =
                idle - _previousIdleTime;

            ulong kernelDifference =
                kernel - _previousKernelTime;

            ulong userDifference =
                user - _previousUserTime;

            _previousIdleTime = idle;
            _previousKernelTime = kernel;
            _previousUserTime = user;

            ulong totalDifference =
                kernelDifference + userDifference;

            if (totalDifference == 0)
            {
                return 0;
            }

            double usage =
                100.0 -
                ((double)idleDifference / totalDifference * 100.0);

            return Math.Clamp(usage, 0, 100);
        }

        private static ulong FileTimeToUInt64(FILETIME fileTime)
        {
            return ((ulong)fileTime.dwHighDateTime << 32)
                   | fileTime.dwLowDateTime;
        }

        private void StartMemoryMonitoring()
        {
            _ = MonitorMemoryUsageAsync();
        }

        private async Task MonitorMemoryUsageAsync()
        {
            while (_monitoringCancellationTokenSource != null &&
                   !_monitoringCancellationTokenSource
                       .IsCancellationRequested)
            {
                UpdateMemoryInformation();

                try
                {
                    await Task.Delay(
                        1000,
                        _monitoringCancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private void UpdateMemoryInformation()
        {
            MEMORYSTATUSEX memoryStatus = new MEMORYSTATUSEX();

            if (!GlobalMemoryStatusEx(ref memoryStatus))
            {
                MemoryUsageText.Text = "Unbekannt";
                MemoryProgressBar.Value = 0;
                return;
            }

            ulong totalMemory =
                memoryStatus.ullTotalPhys;

            ulong availableMemory =
                memoryStatus.ullAvailPhys;

            ulong usedMemory =
                totalMemory - availableMemory;

            double usage =
                totalMemory > 0
                    ? (double)usedMemory / totalMemory * 100.0
                    : 0;

            MemoryUsageText.Text =
                $"{FormatMemorySize(usedMemory)} / " +
                $"{FormatMemorySize(totalMemory)} " +
                $"({usage:0} %)";

            MemoryProgressBar.Value =
                Math.Clamp(usage, 0, 100);
        }

        private static string FormatMemorySize(ulong bytes)
        {
            const double gigabyte =
                1024.0 * 1024.0 * 1024.0;

            const double megabyte =
                1024.0 * 1024.0;

            if (bytes >= gigabyte)
            {
                return $"{bytes / gigabyte:0.0} GB";
            }

            return $"{bytes / megabyte:0} MB";
        }

        [DllImport("kernel32.dll")]
        private static extern bool GetSystemTimes(
            out FILETIME lpIdleTime,
            out FILETIME lpKernelTime,
            out FILETIME lpUserTime);

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetLogicalProcessorInformationEx(
            LOGICAL_PROCESSOR_RELATIONSHIP RelationshipType,
            IntPtr Buffer,
            ref uint ReturnedLength);

        private enum LOGICAL_PROCESSOR_RELATIONSHIP
        {
            RelationProcessorCore = 0,
            RelationNumaNode = 1,
            RelationCache = 2,
            RelationProcessorPackage = 3,
            RelationGroup = 4,
            RelationProcessorDie = 5,
            RelationNumaNodeEx = 6,
            RelationProcessorModule = 7
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        {
            public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
            public uint Size;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(
            ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
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

        protected override void OnNavigatedFrom(
            Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            _monitoringCancellationTokenSource?.Cancel();
            _monitoringCancellationTokenSource?.Dispose();
            _monitoringCancellationTokenSource = null;

            base.OnNavigatedFrom(e);
        }
    }
}
