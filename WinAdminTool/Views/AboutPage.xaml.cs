using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Reflection;
using Windows.System;

namespace WinAdminTool.Views
{
    public sealed partial class AboutPage : Page
    {
        private const string GitHubUrl = "https://github.com/LordNikon999/WinAdminTool";

        public AboutPage()
        {
            InitializeComponent();

            VersionTextBlock.Text =
                $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unbekannt"} · Windows 10 / Windows 11 · x64";
        }

        private async void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(GitHubUrl));
        }
    }
}