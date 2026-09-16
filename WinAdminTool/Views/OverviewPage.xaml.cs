using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
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
