using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

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

        if (tag == "System")
        {
            ContentFrame.Content = new Views.SystemPage();
            return;
        }

        if (tag == "Services")
        {
            ContentFrame.Content = new Views.ServicesPage();
            return;
        }

        if (tag == "Processes")
        {
            ContentFrame.Content = new Views.ProcessesPage();
            return;
        }

        if (tag == "Autostart")
        {
            ContentFrame.Content = new Views.AutostartPage();
            return;
        }

        if (tag == "Events")
        {
            ContentFrame.Content = new Views.EventsPage();
            return;
        }

        if (tag == "Storage")
        {
            ContentFrame.Content = new Views.StoragePage();
            return;
        }

        
        if (tag == "Network")
        {
            ContentFrame.Content = new Views.NetworkPage();
            return;
        }

        if (tag == "Bluetooth")
        {
            ContentFrame.Content = new Views.BluetoothPage();
            return;
        }

        if (tag == "Tools")
        {
            ContentFrame.Content = new Views.ToolsPage();
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
