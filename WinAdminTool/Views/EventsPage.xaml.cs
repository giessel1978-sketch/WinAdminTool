using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinAdminTool.Views
{
    public sealed partial class EventsPage : Page
    {
        public EventsPage()
        {
            this.InitializeComponent();
        }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Ereignisabfrage folgt im nächsten Schritt.
        }

        private void EventsListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (EventsListView.SelectedItem == null)
            {
                DetailPlaceholder.Text = "Wählen Sie ein Ereignis aus.";
                DetailTextBlock.Text = string.Empty;
                return;
            }

            DetailPlaceholder.Text = string.Empty;
            DetailTextBlock.Text = "Ereignisdetails werden im nächsten Schritt geladen.";
        }
    }

}
