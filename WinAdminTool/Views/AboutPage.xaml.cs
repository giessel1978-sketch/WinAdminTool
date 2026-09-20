using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.System;

namespace WinAdminTool.Views
{
    public sealed partial class AboutPage : Page
    {
        private const string GitHubUrl = "https://github.com/LordNikon999/WinAdminTool";

        public AboutPage()
        {
            InitializeComponent();
        }

        private async void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new System.Uri(GitHubUrl));
        }
    }
}