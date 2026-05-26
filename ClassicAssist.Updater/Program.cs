using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace ClassicAssist.Updater
{
    internal static class Program
    {
        [STAThread]
        public static void Main( string[] args )
        {
            if ( args.Contains( "--smoke" ) )
            {
                RunSmoke( args );
                return;
            }
            BuildAvaloniaApp().StartWithClassicDesktopLifetime( args );
        }

        private static void RunSmoke( string[] args )
        {
            try { RunSmokeCore( args ); }
            catch ( InvalidOperationException ex ) when ( ex.Message.Contains( "Dispatcher" ) ) { }
        }

        private static void RunSmokeCore( string[] args )
        {
            App.SmokeMode = true;
            BuildAvaloniaApp().AfterSetup( b =>
            {
                if ( b.Instance?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
                {
                    int ok = 0, fail = 0;
                    foreach ( var t in new[] { typeof( SettingsWindow ), typeof( ProcessesView ) } )
                    {
                        try
                        {
                            var w = (Window) Activator.CreateInstance( t );
                            Console.WriteLine( $"[Updater-smoke] OK {t.Name}" );
                            ok++;
                        }
                        catch ( Exception ex )
                        {
                            var inner = ex.InnerException ?? ex;
                            Console.WriteLine( $"[Updater-smoke] FAIL {t.Name}: {inner.GetType().Name}: {inner.Message}" );
                            Console.WriteLine( $"    at {inner.StackTrace?.Split( '\n' )?[0]?.Trim()}" );
                            fail++;
                        }
                    }
                    Console.WriteLine( $"[Updater-smoke] {ok} ok, {fail} failed (MainWindow excluded — kicks off network update task)" );
                    desktop.Shutdown( fail == 0 ? 0 : 1 );
                }
            } ).StartWithClassicDesktopLifetime( args );
        }

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
