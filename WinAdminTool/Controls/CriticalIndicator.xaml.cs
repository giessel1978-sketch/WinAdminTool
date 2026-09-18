using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinAdminTool.Controls
{
    public sealed partial class CriticalIndicator : UserControl
    {
        private readonly DispatcherQueueTimer _timer;
        private bool _showRed = true;

        public static readonly DependencyProperty LevelProperty =
            DependencyProperty.Register(
                nameof(Level),
                typeof(string),
                typeof(CriticalIndicator),
                new PropertyMetadata(
                    string.Empty,
                    OnLevelChanged));

        public string Level
        {
            get => (string)GetValue(LevelProperty);
            set => SetValue(LevelProperty, value);
        }

        public CriticalIndicator()
        {
            InitializeComponent();

            _timer = DispatcherQueue
                .GetForCurrentThread()
                .CreateTimer();

            _timer.Interval = TimeSpan.FromMilliseconds(800);
            _timer.Tick += Timer_Tick;

            UpdateIndicator();
        }

        private static void OnLevelChanged(
            DependencyObject sender,
            DependencyPropertyChangedEventArgs args)
        {
            if (sender is CriticalIndicator indicator)
            {
                indicator.UpdateIndicator();
            }
        }

        private void UpdateIndicator()
        {
            bool isCritical = string.Equals(
                Level,
                "Kritisch",
                StringComparison.OrdinalIgnoreCase);

            if (isCritical)
            {
                _timer.Start();
                SetCriticalColor();
            }
            else
            {
                _timer.Stop();
                SetNormalColor();
            }
        }

        private void Timer_Tick(
            DispatcherQueueTimer sender,
            object args)
        {
            _showRed = !_showRed;

            Indicator.Fill = new SolidColorBrush(
                _showRed
                    ? Windows.UI.Color.FromArgb(255, 220, 60, 60)
                    : Windows.UI.Color.FromArgb(255, 230, 180, 50));
        }

        private void SetCriticalColor()
        {
            _showRed = true;

            Indicator.Fill = new SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 220, 60, 60));
        }

        private void SetNormalColor()
        {
            Indicator.Fill = new SolidColorBrush(
                GetNormalColor());
        }

        private Windows.UI.Color GetNormalColor()
        {
            return Level switch
            {
                "Fehler" =>
                    Windows.UI.Color.FromArgb(255, 230, 80, 80),

                "Warnung" =>
                    Windows.UI.Color.FromArgb(255, 230, 180, 50),

                "Information" =>
                    Windows.UI.Color.FromArgb(255, 70, 140, 230),

                "Ausführlich" =>
                    Windows.UI.Color.FromArgb(255, 130, 130, 130),

                _ =>
                    Windows.UI.Color.FromArgb(255, 150, 150, 150)
            };
        }
    }
}