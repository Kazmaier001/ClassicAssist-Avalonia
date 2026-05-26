using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ClassicAssist.Launcher
{
    public partial class ShardsWindow : Window
    {
        public ShardsWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load( this );
        }

        private void OpenGithub_OnClick( object sender, RoutedEventArgs e )
        {
            const string url = "https://github.com/Reetus/ClassicAssist-Shards/issues";
            Process.Start( new ProcessStartInfo { FileName = url, UseShellExecute = true } );
        }
    }
}
