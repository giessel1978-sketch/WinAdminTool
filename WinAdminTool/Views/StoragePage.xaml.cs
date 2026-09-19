using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace WinAdminTool.Views
{
    public sealed partial class StoragePage : Page
    {
        private readonly List<StorageItem> _storageItems = new();
        private readonly Dictionary<string, VolumeInfo> _volumes = new();

        public StoragePage()
        {
            InitializeComponent();
            LoadStorage();
        }

        private void LoadStorage()
        {
            try
            {
                LoadVolumes();
                LoadDisksAndPartitions();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Speicherinformationen: {ex}");
            }
        }

        // ============================================================
        // Volumes / Laufwerke
        // ============================================================

        private void LoadVolumes()
        {
            _volumes.Clear();

            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, VolumeName, FileSystem, Size, FreeSpace " +
                "FROM Win32_LogicalDisk " +
                "WHERE DriveType = 3");

            using var results = searcher.Get();

            foreach (ManagementObject volume in results)
            {
                string deviceId =
                    volume["DeviceID"]?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(deviceId))
                    continue;

                long.TryParse(
                    volume["Size"]?.ToString(),
                    out long size);

                long.TryParse(
                    volume["FreeSpace"]?.ToString(),
                    out long freeSpace);

                string volumeName =
                    volume["VolumeName"]?.ToString() ?? string.Empty;

                string fileSystem =
                    volume["FileSystem"]?.ToString() ?? string.Empty;

                _volumes[deviceId] = new VolumeInfo
                {
                    DeviceId = deviceId,
                    VolumeName = volumeName,
                    FileSystem = fileSystem,
                    TotalBytes = size,
                    FreeBytes = freeSpace
                };
            }
        }

        // ============================================================
        // Datenträger laden
        // ============================================================

        private void LoadDisksAndPartitions()
        {
            _storageItems.Clear();

            using var diskSearcher = new ManagementObjectSearcher(
                "SELECT Index, Model, DeviceID, Size, InterfaceType, MediaType " +
                "FROM Win32_DiskDrive");

            using var diskResults = diskSearcher.Get();

            foreach (ManagementObject disk in diskResults)
            {
                int.TryParse(
                    disk["Index"]?.ToString(),
                    out int diskIndex);

                long.TryParse(
                    disk["Size"]?.ToString(),
                    out long diskSize);

                string model =
                    disk["Model"]?.ToString() ??
                    "Unbekannter Datenträger";

                string deviceId =
                    disk["DeviceID"]?.ToString() ??
                    string.Empty;

                string interfaceType =
                    disk["InterfaceType"]?.ToString() ??
                    string.Empty;

                string mediaType =
                    disk["MediaType"]?.ToString() ??
                    string.Empty;

                var diskItem = new StorageItem
                {
                    Type = StorageItemType.Disk,
                    Name = $"Datenträger {diskIndex}",
                    Description = model,
                    Device = deviceId,
                    TotalBytes = diskSize,
                    DiskIndex = diskIndex,
                    InterfaceType = interfaceType,
                    MediaType = mediaType
                };

                LoadPartitionsForDisk(
                    diskItem,
                    diskIndex);

                _storageItems.Add(diskItem);
            }

            StorageTreeView.RootNodes.Clear();

            foreach (StorageItem disk in _storageItems)
            {
                var diskNode = new TreeViewNode
                {
                    Content = disk
                };

                foreach (StorageItem partition in disk.Children)
                {
                    diskNode.Children.Add(
                        new TreeViewNode
                        {
                            Content = partition
                        });
                }

                StorageTreeView.RootNodes.Add(diskNode);
            }
        }

        // ============================================================
        // Partitionen laden
        // ============================================================

        private void LoadPartitionsForDisk(
            StorageItem diskItem,
            int diskIndex)
        {
            using var partitionSearcher = new ManagementObjectSearcher(
                $"SELECT DeviceID, Name, Description, Index, Size, " +
                $"Type, BootPartition, PrimaryPartition, DiskIndex " +
                $"FROM Win32_DiskPartition " +
                $"WHERE DiskIndex = {diskIndex}");

            using var partitionResults = partitionSearcher.Get();

            foreach (ManagementObject partition in partitionResults)
            {
                int.TryParse(
                    partition["Index"]?.ToString(),
                    out int partitionIndex);

                long.TryParse(
                    partition["Size"]?.ToString(),
                    out long partitionSize);

                string partitionDeviceId =
                    partition["DeviceID"]?.ToString() ??
                    string.Empty;

                string description =
                    partition["Description"]?.ToString() ??
                    "Partition";

                string type =
                    partition["Type"]?.ToString() ??
                    string.Empty;

                bool.TryParse(
                    partition["BootPartition"]?.ToString(),
                    out bool bootPartition);

                bool.TryParse(
                    partition["PrimaryPartition"]?.ToString(),
                    out bool primaryPartition);

                var partitionItem = new StorageItem
                {
                    Type = StorageItemType.Partition,
                    Name = $"Partition {partitionIndex}",
                    Description = description,
                    Device = partitionDeviceId,
                    TotalBytes = partitionSize,
                    DiskIndex = diskIndex,
                    PartitionIndex = partitionIndex,
                    PartitionType = type,
                    BootPartition = bootPartition,
                    PrimaryPartition = primaryPartition
                };

                ApplyVolumeInformation(partitionItem);

                diskItem.Children.Add(partitionItem);
            }
        }

        // ============================================================
        // Volume einer Partition zuordnen
        // ============================================================

        private void ApplyVolumeInformation(StorageItem partition)
        {
            if (string.IsNullOrWhiteSpace(partition.Device))
                return;

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Antecedent, Dependent " +
                    "FROM Win32_LogicalDiskToPartition");

                using var results = searcher.Get();

                foreach (ManagementObject association in results)
                {
                    string antecedent =
                        association["Antecedent"]?.ToString() ??
                        string.Empty;

                    string dependent =
                        association["Dependent"]?.ToString() ??
                        string.Empty;

                    string associatedPartition =
                        ExtractDeviceId(antecedent);

                    string associatedVolume =
                        ExtractDeviceId(dependent);

                    if (!string.Equals(
                            associatedPartition,
                            partition.Device,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(associatedVolume))
                        continue;

                    if (!_volumes.TryGetValue(
                            associatedVolume,
                            out VolumeInfo? volume))
                    {
                        continue;
                    }

                    partition.VolumeDeviceId =
                        volume.DeviceId;

                    partition.VolumeName =
                        volume.VolumeName;

                    partition.FileSystem =
                        volume.FileSystem;

                    partition.FreeBytes =
                        volume.FreeBytes;

                    break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Fehler bei Volume-Zuordnung für " +
                    $"{partition.Device}: {ex}");
            }
        }

        // ============================================================
        // DeviceID aus WMI-Objektpfad
        // ============================================================

        private static string ExtractDeviceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            const string marker = "DeviceID=";

            int markerIndex = value.IndexOf(
                marker,
                StringComparison.OrdinalIgnoreCase);

            if (markerIndex < 0)
                return string.Empty;

            int firstQuote = value.IndexOf(
                '"',
                markerIndex + marker.Length);

            if (firstQuote < 0)
                return string.Empty;

            int secondQuote = value.IndexOf(
                '"',
                firstQuote + 1);

            if (secondQuote < 0)
                return string.Empty;

            return value.Substring(
                firstQuote + 1,
                secondQuote - firstQuote - 1);
        }

        // ============================================================
        // Auswahl
        // ============================================================

        private void StorageTreeView_SelectionChanged(
            TreeView sender,
            TreeViewSelectionChangedEventArgs args)
        {
            if (args.AddedItems.Count == 0)
            {
                ShowNoSelection();
                return;
            }

            if (args.AddedItems[0] is not TreeViewNode node)
            {
                ShowNoSelection();
                return;
            }

            if (node.Content is not StorageItem item)
            {
                ShowNoSelection();
                return;
            }

            ShowDetails(item);
        }

        // ============================================================
        // Detailbereich
        // ============================================================

        private void ShowDetails(StorageItem item)
        {
            DetailPlaceholder.Visibility =
                Visibility.Collapsed;

            DetailPanel.Visibility =
                Visibility.Visible;

            DetailName.Text =
                item.Name;

            DetailDescription.Text =
                item.Description;

            if (item.Type == StorageItemType.Disk)
            {
                ShowDiskDetails(item);
            }
            else
            {
                ShowPartitionDetails(item);
            }
        }

        // ============================================================
        // Datenträgerdetails
        // ============================================================

        private void ShowDiskDetails(StorageItem item)
        {
            DetailDevice.Text =
                $"Gerät: {item.Device}";

            DetailVolume.Text =
                "Laufwerk: –";

            DetailFileSystem.Text =
                "Dateisystem: –";

            DetailTotal.Text =
                $"Größe: {FormatBytes(item.TotalBytes)}";

            DetailUsed.Text =
                "Belegt: –";

            DetailFree.Text =
                "Frei: –";

            DetailUsage.Text =
                "Auslastung: –";

            DetailInterface.Text =
                string.IsNullOrWhiteSpace(item.InterfaceType)
                    ? "Schnittstelle: –"
                    : $"Schnittstelle: {item.InterfaceType}";

            DetailMediaType.Text =
                string.IsNullOrWhiteSpace(item.MediaType)
                    ? "Medientyp: –"
                    : $"Medientyp: {item.MediaType}";

            DetailPartitionType.Text =
                string.Empty;

            DetailPrimaryPartition.Text =
                string.Empty;

            DetailBootPartition.Text =
                string.Empty;

            DetailHardwareTitle.Text =
                "Hardwareinformationen";
        }

        // ============================================================
        // Partitiondetails
        // ============================================================

        private void ShowPartitionDetails(StorageItem item)
        {
            DetailDevice.Text =
                $"Gerät: {item.Device}";

            DetailVolume.Text =
                string.IsNullOrWhiteSpace(item.VolumeDeviceId)
                    ? "Laufwerk: –"
                    : $"Laufwerk: {item.VolumeDeviceId}";

            DetailFileSystem.Text =
                string.IsNullOrWhiteSpace(item.FileSystem)
                    ? "Dateisystem: –"
                    : $"Dateisystem: {item.FileSystem}";

            DetailTotal.Text =
                $"Größe: {FormatBytes(item.TotalBytes)}";

            DetailUsed.Text =
                item.UsedBytes > 0
                    ? $"Belegt: {FormatBytes(item.UsedBytes)}"
                    : "Belegt: –";

            DetailFree.Text =
                item.FreeBytes > 0
                    ? $"Frei: {FormatBytes(item.FreeBytes)}"
                    : "Frei: –";

            DetailUsage.Text =
                item.TotalBytes > 0 &&
                item.FreeBytes >= 0
                    ? $"Auslastung: {item.UsagePercent:0}%"
                    : "Auslastung: –";

            DetailInterface.Text =
                string.Empty;

            DetailMediaType.Text =
                string.Empty;

            DetailPartitionType.Text =
                string.IsNullOrWhiteSpace(item.PartitionType)
                    ? "Partitionstyp: –"
                    : $"Partitionstyp: {item.PartitionType}";

            DetailPrimaryPartition.Text =
                $"Primäre Partition: " +
                $"{(item.PrimaryPartition ? "Ja" : "Nein")}";

            DetailBootPartition.Text =
                $"Bootpartition: " +
                $"{(item.BootPartition ? "Ja" : "Nein")}";

            DetailHardwareTitle.Text =
                "Partitionsinformationen";
        }

        // ============================================================
        // Keine Auswahl
        // ============================================================

        private void ShowNoSelection()
        {
            DetailPlaceholder.Visibility =
                Visibility.Visible;

            DetailPanel.Visibility =
                Visibility.Collapsed;
        }

        // ============================================================
        // Formatierung
        // ============================================================

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
                return "–";

            const double gb =
                1024.0 * 1024.0 * 1024.0;

            const double tb =
                gb * 1024.0;

            if (bytes >= tb)
                return $"{bytes / tb:0.0} TB";

            return $"{bytes / gb:0.0} GB";
        }

        // ============================================================
        // Datenmodelle
        // ============================================================

        private enum StorageItemType
        {
            Disk,
            Partition
        }

        private sealed class VolumeInfo
        {
            public string DeviceId { get; set; } = string.Empty;

            public string VolumeName { get; set; } =
                string.Empty;

            public string FileSystem { get; set; } =
                string.Empty;

            public long TotalBytes { get; set; }

            public long FreeBytes { get; set; }
        }

        private sealed class StorageItem
        {
            public StorageItemType Type { get; set; }

            public string Name { get; set; } =
                string.Empty;

            public string Description { get; set; } =
                string.Empty;

            public string Device { get; set; } =
                string.Empty;

            public long TotalBytes { get; set; }

            public long FreeBytes { get; set; }

            public int DiskIndex { get; set; }

            public int PartitionIndex { get; set; }

            public string InterfaceType { get; set; } =
                string.Empty;

            public string MediaType { get; set; } =
                string.Empty;

            public string PartitionType { get; set; } =
                string.Empty;

            public bool BootPartition { get; set; }

            public bool PrimaryPartition { get; set; }

            public string VolumeDeviceId { get; set; } =
                string.Empty;

            public string VolumeName { get; set; } =
                string.Empty;

            public string FileSystem { get; set; } =
                string.Empty;

            public List<StorageItem> Children { get; } =
                new();

            public long UsedBytes =>
                Type == StorageItemType.Partition &&
                TotalBytes > 0 &&
                FreeBytes >= 0
                    ? TotalBytes - FreeBytes
                    : 0;

            public double UsagePercent =>
                Type == StorageItemType.Partition &&
                TotalBytes > 0 &&
                FreeBytes >= 0
                    ? ((double)(TotalBytes - FreeBytes)
                        / TotalBytes) * 100.0
                    : 0;

            public string TotalSpace =>
                FormatBytes(TotalBytes);

            public string UsedSpace =>
                Type == StorageItemType.Partition &&
                UsedBytes > 0
                    ? FormatBytes(UsedBytes)
                    : "–";

            public string UsageText =>
                Type == StorageItemType.Partition &&
                TotalBytes > 0 &&
                FreeBytes >= 0
                    ? $"{UsagePercent:0}%"
                    : "–";
        }
    }
}
