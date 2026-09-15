using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinAdminTool;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        MainNavigation.SelectedItem = MainNavigation.MenuItems[0];
    }

    private void MainNavigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem selectedItem)
            return;

        string tag = selectedItem.Tag?.ToString() ?? string.Empty;

        if (tag == "Overview")
        {
            ContentFrame.Content = new Views.OverviewPage();
            return;
        }

        string title = selectedItem.Content?.ToString() ?? "WinAdminTool";

        ContentFrame.Content = new TextBlock
        {
            Text = title,
            FontSize = 32,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

}