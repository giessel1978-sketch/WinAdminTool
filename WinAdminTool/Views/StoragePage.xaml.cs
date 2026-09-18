using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.ConstrainedExecution;

namespace WinAdminTool.Views;

public sealed partial class StoragePage : Page
{
    private readonly Dictionary<string, TreeViewNode> _disks = new();

    public StoragePage()
    {
        InitializeComponent();

        try
        {
            LoadStorage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "StoragePage Fehler: " + ex);
        }
    }

    private void LoadStorage()
    {
        StorageTreeView.RootNodes.Clear();
        _disks.Clear();

        var diskNodes = new Dictionary<string, TreeViewNode>();

        // ---------------------------------------------------------
        // Physische Datenträger
        // ---------------------------------------------------------

        using (var searcher = new ManagementObjectSearcher(
            "SELECT Index, Model, Size, InterfaceType FROM Win32_DiskDrive"))
        {
            foreach (ManagementObject disk in searcher.Get())
            {
                string index =
                    disk["Index"]?.ToString() ?? "";

                string model =
                    disk["Model"]?.ToString()
                    ?? "Unbekannter Datenträger";

                string size =
                    FormatBytes(disk["Size"]);

                string interfaceType =
                    disk["InterfaceType"]?.ToString() ?? "-";

                var item = new StorageItem
                {
                    Name = $"Datenträger {index}",
                    Description = model,
                    Device = $"Datenträger {index}",
                    FileSystem = "-",
                    TotalSpace = size,
                    UsedSpace = "-",
                    FreeSpace = "-",
                    UsageText = "-",
                    UsagePercent = 0,
                    PartitionType = "-",
                    InterfaceType = interfaceType
                };

                var node = new TreeViewNode
                {
                    Content = item
                };

                StorageTreeView.RootNodes.Add(node);

                diskNodes[index] = node;
                _disks[index] = node;
            }
        }

        // ---------------------------------------------------------
        // Partitionen
        // ---------------------------------------------------------

        using (var searcher = new ManagementObjectSearcher(
            "SELECT DiskIndex, Index, Name, Size, Type, BootPartition " +
            "FROM Win32_DiskPartition"))
        {
            foreach (ManagementObject partition in searcher.Get())
            {
                string diskIndex =
                    partition["DiskIndex"]?.ToString() ?? "";

                if (!diskNodes.TryGetValue(
                        diskIndex,
                        out TreeViewNode? diskNode))
                {
                    continue;
                }

                string partitionIndex =
                    partition["Index"]?.ToString() ?? "";

                string deviceName =
                    partition["Name"]?.ToString() ?? "";

                string partitionSize =
                    FormatBytes(partition["Size"]);

                string partitionType =
                    partition["Type"]?.ToString() ?? "-";

                bool bootPartition =
                    partition["BootPartition"] is bool value && value;

                var item = new StorageItem
                {
                    Name = $"Partition {partitionIndex}",
                    Description = partitionType,
                    Device = deviceName,
                    FileSystem = "-",
                    TotalSpace = partitionSize,
                    UsedSpace = "-",
                    FreeSpace = "-",
                    UsageText = "-",
                    UsagePercent = 0,
                    PartitionType = partitionType,
                    BootPartition = bootPartition
                };

                // Versuchen, ein vorhandenes Volume
                // dieser Partition zuzuordnen.
                TryAddVolumeInformation(
                    item,
                    deviceName);

                var partitionNode = new TreeViewNode
                {
                    Content = item
                };

                diskNode.Children.Add(partitionNode);
            }
        }
    }

    // -------------------------------------------------------------
    // Volumeinformationen zur Partition ermitteln
    // -------------------------------------------------------------

    private void TryAddVolumeInformation(
        StorageItem item,
        string partitionDeviceName)
    {
        if (string.IsNullOrWhiteSpace(partitionDeviceName))
            return;

        try
        {
            using var volumeSearcher =
                new ManagementObjectSearcher(
                    "SELECT DeviceID, VolumeName, FileSystem, Size, FreeSpace " +
                    "FROM Win32_LogicalDisk");

            foreach (ManagementObject volume
                     in volumeSearcher.Get())
            {
                string driveLetter =
                    volume["DeviceID"]?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(driveLetter))
                    continue;

                if (!IsVolumeOnPartition(
                        driveLetter,
                        partitionDeviceName))
                {
                    continue;
                }

                string volumeName =
                    volume["VolumeName"]?.ToString() ?? "";

                string fileSystem =
                    volume["FileSystem"]?.ToString() ?? "-";

                ulong totalBytes =
                    ToUInt64(volume["Size"]);

                ulong freeBytes =
                    ToUInt64(volume["FreeSpace"]);

                ulong usedBytes =
                    totalBytes > freeBytes
                        ? totalBytes - freeBytes
                        : 0;

                double usagePercent = 0;

                if (totalBytes > 0)
                {
                    usagePercent =
                        usedBytes * 100.0 / totalBytes;
                }

                item.DriveLetter =
                    driveLetter;

                item.VolumeName =
                    string.IsNullOrWhiteSpace(volumeName)
                        ? "-"
                        : volumeName;

                item.FileSystem =
                    string.IsNullOrWhiteSpace(fileSystem)
                        ? "-"
                        : fileSystem;

                item.TotalSpace =
                    FormatBytes(totalBytes);

                item.UsedSpace =
                    FormatBytes(usedBytes);

                item.FreeSpace =
                    FormatBytes(freeBytes);

                item.UsagePercent =
                    usagePercent;

                item.UsageText =
                    $"{usagePercent:0}%";

                // Name in der Baumansicht
                if (!string.IsNullOrWhiteSpace(volumeName))
                {
                    item.Name =
                        $"{driveLetter} {volumeName}";
                }
                else
                {
                    item.Name =
                        driveLetter;
                }

                return;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "Volumezuordnung Fehler: " + ex);
        }
    }

    // -------------------------------------------------------------
    // Prüft, ob ein Volume zu einer Partition gehört
    // -------------------------------------------------------------

    private bool IsVolumeOnPartition(
        string driveLetter,
        string partitionDeviceName)
    {
        try
        {
            using var associationSearcher =
                new ManagementObjectSearcher(
                    "SELECT Antecedent, Dependent " +
                    "FROM Win32_LogicalDiskToPartition");

            foreach (ManagementObject association
                     in associationSearcher.Get())
            {
                string antecedent =
                    association["Antecedent"]?.ToString() ?? "";

                string dependent =
                    association["Dependent"]?.ToString() ?? "";

                if (!antecedent.Contains(
                        partitionDeviceName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!dependent.Contains(
                        $"DeviceID=\"{driveLetter}\"",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "Partitionszuordnung Fehler: " + ex);
        }

        return false;
    }

    // -------------------------------------------------------------
    // Auswahl im TreeView
    // -------------------------------------------------------------

    private void StorageTreeView_SelectionChanged(
        TreeView sender,
        TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.Count == 0)
            return;

        object? selected =
            args.AddedItems[0];

        StorageItem? item = null;

        if (selected is TreeViewNode node)
        {
            item = node.Content as StorageItem;
        }
        else if (selected is StorageItem storageItem)
        {
            item = storageItem;
        }

        if (item == null)
            return;

        // ---------------------------------------------------------
        // Details füllen
        // ---------------------------------------------------------

        DetailName.Text =
            item.Name;

        DetailDescription.Text =
            item.Description;

        DetailDevice.Text =
            $"Gerät: {item.Device}";

        DetailFileSystem.Text =
            $"Dateisystem: {item.FileSystem}";

        DetailTotal.Text =
            $"Gesamt: {item.TotalSpace}";

        DetailUsed.Text =
            $"Belegt: {item.UsedSpace}";

        DetailFree.Text =
            $"Frei: {item.FreeSpace}";

        DetailUsage.Text =
            $"Auslastung: {item.UsageText}";

        DetailPartitionType.Text =
            $"Partitionstyp: {item.PartitionType}";

        // ---------------------------------------------------------
        // Detailbereich sichtbar machen
        // ---------------------------------------------------------

        DetailPlaceholder.Visibility =
            Visibility.Collapsed;

        DetailPanel.Visibility =
            Visibility.Visible;
    }

    // -------------------------------------------------------------
    // Hilfsfunktionen
    // -------------------------------------------------------------

    private static ulong ToUInt64(object? value)
    {
        if (value == null)
            return 0;

        try
        {
            return Convert.ToUInt64(value);
        }
        catch
        {
            return 0;
        }
    }

    private static string FormatBytes(object? value)
    {
        ulong bytes =
            ToUInt64(value);

        return FormatBytes(bytes);
    }

    private static string FormatBytes(ulong bytes)
    {
        if (bytes == 0)
            return "-";

        const double KB = 1024.0;
        const double MB = KB * 1024.0;
        const double GB = MB * 1024.0;
        const double TB = GB * 1024.0;

        if (bytes >= TB)
            return $"{bytes / TB:0.0} TB";

        if (bytes >= GB)
            return $"{bytes / GB:0.0} GB";

        if (bytes >= MB)
            return $"{bytes / MB:0.0} MB";

        if (bytes >= KB)
            return $"{bytes / KB:0.0} KB";

        return $"{bytes} B";
    }

    // -------------------------------------------------------------
    // Datenmodell
    // -------------------------------------------------------------

    private sealed class StorageItem
    {
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        public string Device { get; set; } = "";

        public string DriveLetter { get; set; } = "";

        public string VolumeName { get; set; } = "-";

        public string FileSystem { get; set; } = "-";

        public string TotalSpace { get; set; } = "-";

        public string UsedSpace { get; set; } = "-";

        public string FreeSpace { get; set; } = "-";

        public double UsagePercent { get; set; }

        public string UsageText { get; set; } = "-";

        public string PartitionType { get; set; } = "-";

        public string InterfaceType { get; set; } = "-";

        public bool BootPartition { get; set; }
    }
}
