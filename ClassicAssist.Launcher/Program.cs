using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace ClassicAssist.Launcher
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
            var builder = BuildAvaloniaApp().AfterSetup( b =>
            {
                if ( b.Instance?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
                {
                    int ok = 0, fail = 0;
                    foreach ( var t in new[] { typeof( MainWindow ), typeof( OptionsWindow ), typeof( ShardsWindow ) } )
                    {
                        try
                        {
                            var w = (Window) Activator.CreateInstance( t );
                            Console.WriteLine( $"[Launcher-smoke] OK {t.Name}" );
                            ok++;
                        }
                        catch ( Exception ex )
                        {
                            var inner = ex.InnerException ?? ex;
                            Console.WriteLine( $"[Launcher-smoke] FAIL {t.Name}: {inner.GetType().Name}: {inner.Message}" );
                            Console.WriteLine( $"    at {inner.StackTrace?.Split( '\n' )?[0]?.Trim()}" );
                            fail++;
                        }
                    }
                    Console.WriteLine( $"[Launcher-smoke] {ok} ok, {fail} failed" );
                    desktop.Shutdown( fail == 0 ? 0 : 1 );
                }
            } );
            builder.StartWithClassicDesktopLifetime( args );
        }

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
