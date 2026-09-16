using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace WinAdminTool.Views
{
    public sealed partial class SystemPage : Page
    {
        public SystemPage()
        {
            this.InitializeComponent();

            LoadSystemInformation();
            LoadCpuInformation();
            LoadMemoryInformation();
            LoadHardwareInformation();
            LoadBiosInformation();
        }

        private void LoadBiosInformation()
        {
            try
            {
                using RegistryKey? key =
                    Registry.LocalMachine.OpenSubKey(
                        @"HARDWARE\DESCRIPTION\System\BIOS");

                if (key == null)
                {
                    BiosManufacturerText.Text = "Unbekannt";
                    BiosVersionText.Text = "Unbekannt";
                    BiosDateText.Text = "Unbekannt";
                    return;
                }

                string? manufacturer =
                    key.GetValue("BIOSVendor") as string;

                string? version =
                    key.GetValue("BIOSVersion") as string;

                string? date =
                    key.GetValue("BIOSReleaseDate") as string;

                BiosManufacturerText.Text =
                    string.IsNullOrWhiteSpace(manufacturer)
                        ? "Unbekannt"
                        : manufacturer.Trim();

                BiosVersionText.Text =
                    string.IsNullOrWhiteSpace(version)
                        ? "Unbekannt"
                        : version.Trim();

                BiosDateText.Text =
                    string.IsNullOrWhiteSpace(date)
                        ? "Unbekannt"
                        : date.Trim();
            }
            catch
            {
                BiosManufacturerText.Text = "Unbekannt";
                BiosVersionText.Text = "Unbekannt";
                BiosDateText.Text = "Unbekannt";
            }
        }
        private void LoadHardwareInformation()
        {
            try
            {
                using RegistryKey? key =
                    Registry.LocalMachine.OpenSubKey(
                        @"HARDWARE\DESCRIPTION\System\BIOS");

                if (key == null)
                {
                    SystemManufacturerText.Text = "Unbekannt";
                    SystemModelText.Text = "Unbekannt";
                    return;
                }

                string? manufacturer =
                    key.GetValue("SystemManufacturer") as string;

                string? model =
                    key.GetValue("SystemProductName") as string;

                SystemManufacturerText.Text =
                    string.IsNullOrWhiteSpace(manufacturer)
                        ? "Unbekannt"
                        : manufacturer.Trim();

                SystemModelText.Text =
                    string.IsNullOrWhiteSpace(model)
                        ? "Unbekannt"
                        : model.Trim();
            }
            catch
            {
                SystemManufacturerText.Text = "Unbekannt";
                SystemModelText.Text = "Unbekannt";
            }
        }
        private void LoadMemoryInformation()
        {
            MEMORYSTATUSEX memoryStatus = new MEMORYSTATUSEX();

            if (!GlobalMemoryStatusEx(ref memoryStatus))
            {
                MemoryInformationText.Text = "Unbekannt";
                return;
            }

            MemoryInformationText.Text =
                $"{FormatMemorySize(memoryStatus.ullTotalPhys)} " +
                "installierter Arbeitsspeicher";
        }

        private static string FormatMemorySize(ulong bytes)
        {
            const double gigabyte =
                1024.0 * 1024.0 * 1024.0;

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

                WindowsEditionText.Text =
                    GetWindowsEdition();
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
                using RegistryKey? key =
                    Registry.LocalMachine.OpenSubKey(
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

            IntPtr buffer =
                Marshal.AllocHGlobal((int)length);

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

        private string GetWindowsEdition()
        {
            try
            {
                using RegistryKey? key =
                    Registry.LocalMachine.OpenSubKey(
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
                using RegistryKey? key =
                    Registry.LocalMachine.OpenSubKey(
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
                using RegistryKey? key =
                    Registry.LocalMachine.OpenSubKey(
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

        [DllImport(
            "kernel32.dll",
            SetLastError = true)]
        private static extern bool GetLogicalProcessorInformationEx(
            LOGICAL_PROCESSOR_RELATIONSHIP RelationshipType,
            IntPtr Buffer,
            ref uint ReturnedLength);

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
                dwLength =
                    (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
            }
        }
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
    }
}