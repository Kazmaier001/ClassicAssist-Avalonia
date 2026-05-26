using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Exceptionless;

namespace ClassicAssist.Launcher
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load( this );
        }

        public override void OnFrameworkInitializationCompleted()
        {
            ExceptionlessClient.Default.Configuration.DefaultData.Add( "Locale",
                Thread.CurrentThread.CurrentUICulture.Name );
            ExceptionlessClient.Default.Startup( "T8v0i7nL90cVRc4sr2pgo5hviThMPRF3OtQ0bK60" );

            if ( ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
