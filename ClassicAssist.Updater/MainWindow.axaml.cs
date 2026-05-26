using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ClassicAssist.Updater
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load( this );
        }

        private void OpenReleases_OnClick( object sender, RoutedEventArgs e )
        {
            const string url = "https://github.com/Reetus/ClassicAssist/releases";
            Process.Start( new ProcessStartInfo { FileName = url, UseShellExecute = true } );
        }
    }
}
