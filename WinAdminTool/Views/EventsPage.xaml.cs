using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinAdminTool.Views
{
    public sealed partial class EventsPage : Page
    {
        private readonly ObservableCollection<EventItem> _events = new();
        private readonly List<EventItem> _allEvents = new();

        private bool _isLoading;

        public EventsPage()
        {
            InitializeComponent();

            EventsListView.ItemsSource = _events;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadEventsAsync();
        }

        private async void LogComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _isLoading)
                return;

            await LoadEventsAsync();
        }

        private void LevelComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _isLoading)
                return;

            ApplyFilters();
        }

        private void SearchTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (!IsLoaded || _isLoading)
                return;

            ApplyFilters();
        }

        private async Task LoadEventsAsync()
        {
            if (_isLoading)
                return;

            _isLoading = true;
            RefreshButton.IsEnabled = false;

            try
            {
                _events.Clear();
                _allEvents.Clear();

                string logName = GetSelectedLogName();

                await Task.Run(() =>
                {
                    EventLogQuery query = new EventLogQuery(
                        logName,
                        PathType.LogName,
                        "*");

                    query.ReverseDirection = true;

                    using EventLogReader reader = new EventLogReader(query);

                    int count = 0;

                    while (count < 500)
                    {
                        EventRecord? record = reader.ReadEvent();

                        if (record == null)
                            break;

                        try
                        {
                            EventItem item = new EventItem
                            {
                                TimeCreated = GetTimeCreated(record),
                                Level = GetLevelText(record),
                                ProviderName = GetProviderName(record),
                                Id = GetRecordId(record),
                                Message = GetMessage(record),
                                MachineName = GetMachineName(record),
                                LogName = GetLogName(record),
                                Task = GetTaskDisplayName(record),
                                Opcode = GetOpcodeDisplayName(record),
                                UserId = GetUserId(record),
                                Version = GetVersion(record)
                            };

                            _allEvents.Add(item);
                            count++;
                        }
                        catch
                        {
                            // Einzelnes problematisches Ereignis überspringen.
                        }
                        finally
                        {
                            record.Dispose();
                        }
                    }
                });

                ApplyFilters();
            }
            catch (Exception ex)
            {
                _events.Clear();

                _events.Add(new EventItem
                {
                    TimeCreated = "-",
                    Level = "Fehler",
                    ProviderName = "WinAdminTool",
                    Id = "-",
                    Message =
                        $"Fehler beim Laden der Ereignisse: {ex.Message}"
                });
            }
            finally
            {
                RefreshButton.IsEnabled = true;
                _isLoading = false;
            }
        }

        private void ApplyFilters()
        {
            string selectedLevel = GetSelectedLevel();

            string searchText =
                SearchTextBox.Text?.Trim() ?? string.Empty;

            IEnumerable<EventItem> filtered = _allEvents;

            if (!string.Equals(
                    selectedLevel,
                    "Alle",
                    StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(item =>
                    string.Equals(
                        item.Level,
                        selectedLevel,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered.Where(item =>
                    item.ProviderName.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    item.Id.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    item.Message.Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase));
            }

            _events.Clear();

            foreach (EventItem item in filtered)
            {
                _events.Add(item);
            }

            if (_events.Count == 0)
            {
                DetailPlaceholder.Text = "Keine Ereignisse gefunden.";
            }
            else
            {
                DetailPlaceholder.Text =
                    "Wählen Sie ein Ereignis aus.";
            }

            DetailTextBlock.Text = string.Empty;
        }

        private string GetSelectedLogName()
        {
            if (LogComboBox.SelectedItem is ComboBoxItem item)
            {
                string? content = item.Content?.ToString();

                return content switch
                {
                    "Application" => "Application",
                    "Security" => "Security",
                    _ => "System"
                };
            }

            return "System";
        }

        private string GetSelectedLevel()
        {
            if (LevelComboBox.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString() ?? "Alle";
            }

            return "Alle";
        }

        private static string GetLevelText(EventRecord record)
        {
            try
            {
                string? displayName = record.LevelDisplayName;

                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName;
            }
            catch
            {
            }

            try
            {
                return record.Level switch
                {
                    1 => "Kritisch",
                    2 => "Fehler",
                    3 => "Warnung",
                    4 => "Information",
                    5 => "Ausführlich",
                    _ => "Unbekannt"
                };
            }
            catch
            {
                return "Unbekannt";
            }
        }

        private static string GetTimeCreated(EventRecord record)
        {
            try
            {
                return record.TimeCreated?.ToString(
                    "dd.MM.yyyy HH:mm:ss") ?? "-";
            }
            catch
            {
                return "-";
            }
        }

        private static string GetProviderName(EventRecord record)
        {
            try
            {
                return record.ProviderName ?? "-";
            }
            catch
            {
                return "-";
            }
        }

        private static string GetMachineName(EventRecord record)
        {
            try
            {
                return record.MachineName ?? "-";
            }
            catch
            {
                return "-";
            }
        }

        private static string GetLogName(EventRecord record)
        {
            try
            {
                return record.LogName ?? "-";
            }
            catch
            {
                return "-";
            }
        }

        private static string GetTaskDisplayName(EventRecord record)
        {
            try
            {
                return record.TaskDisplayName ?? "-";
            }
            catch
            {
                return "-";
            }
        }

        private static string GetOpcodeDisplayName(EventRecord record)
        {
            try
            {
                return record.OpcodeDisplayName ?? "-";
            }
            catch
            {
                return "-";
            }
        }

        private static string GetUserId(EventRecord record)
        {
            try
            {
                return record.UserId?.ToString() ?? "-";
            }
            catch
            {
                return "-";
            }
        }

        private static string GetRecordId(EventRecord record)
        {
            try
            {
                return record.Id.ToString();
            }
            catch
            {
                return "-";
            }
        }

        private static string GetVersion(EventRecord record)
        {
            try
            {
                return record.Version?.ToString() ?? "-";
            }
            catch
            {
                return "-";
            }
        }

        private static string GetMessage(EventRecord record)
        {
            try
            {
                string? message = record.FormatDescription();

                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }
            catch
            {
                // Manche Ereignisse besitzen keine verfügbare
                // Provider-/Nachrichtenressource.
            }

            return "-";
        }

        private void EventsListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (EventsListView.SelectedItem is not EventItem item)
            {
                DetailPlaceholder.Text =
                    "Wählen Sie ein Ereignis aus.";

                DetailTextBlock.Text = string.Empty;
                return;
            }

            DetailPlaceholder.Text = string.Empty;

            DetailTextBlock.Text =
                $"Zeitpunkt: {item.TimeCreated}\n\n" +
                $"Ebene: {item.Level}\n\n" +
                $"Quelle: {item.ProviderName}\n\n" +
                $"Ereignis-ID: {item.Id}\n\n" +
                $"Computer: {item.MachineName}\n\n" +
                $"Protokoll: {item.LogName}\n\n" +
                $"Aufgabe: {item.Task}\n\n" +
                $"Opcode: {item.Opcode}\n\n" +
                $"Benutzer: {item.UserId}\n\n" +
                $"Version: {item.Version}\n\n" +
                $"Meldung:\n{item.Message}";
        }
    }

    public class EventItem
    {
        public string TimeCreated { get; set; } = "-";

        public string Level { get; set; } = "-";

        public string ProviderName { get; set; } = "-";

        public string Id { get; set; } = "-";

        public string Message { get; set; } = "-";

        public string MachineName { get; set; } = "-";

        public string LogName { get; set; } = "-";

        public string Task { get; set; } = "-";

        public string Opcode { get; set; } = "-";

        public string UserId { get; set; } = "-";

        public string Version { get; set; } = "-";
    }
}