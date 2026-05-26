using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ClassicAssist.Shared;
using ClassicAssist.Updater.Properties;
using CommandLine;
using Exceptionless;
using IOPath = System.IO.Path;

namespace ClassicAssist.Updater
{
    public partial class App : Application
    {
        public static CommandLineOptions CurrentOptions { get; set; }
        public static UpdaterSettings UpdaterSettings { get; set; }
        public static bool SmokeMode { get; set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load( this );
        }

        public override void OnFrameworkInitializationCompleted()
        {
            string[] args = Environment.GetCommandLineArgs();
            string[] appArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();

            // Exceptionless was previously gated on Settings.Default.ExceptionlessKey; the
            // Settings.settings file isn't part of this project (and Properties.Settings is a
            // WPF/WinForms-only generated class). Hardcoding nothing for now — wire up if needed.

            Parser.Default.ParseArguments<CommandLineOptions>( appArgs ).WithParsed( o => CurrentOptions = o );

            if ( CurrentOptions != null && string.IsNullOrEmpty( CurrentOptions.Path ) )
            {
                CurrentOptions.Path = Environment.CurrentDirectory;
            }

            UpdaterSettings = UpdaterSettings.Load( CurrentOptions?.Path ?? Environment.CurrentDirectory );

            if ( CurrentOptions != null && CurrentOptions.CurrentVersion == null )
            {
                string dllPath = IOPath.Combine( CurrentOptions.Path, "ClassicAssist.dll" );
                try
                {
                    CurrentOptions.CurrentVersion = VersionHelpers.GetProductVersion( dllPath ).ToString();
                }
                catch ( Exception )
                {
                    CurrentOptions.Force = true;
                }
            }

            if ( ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
            {
                if ( !SmokeMode )
                    desktop.MainWindow = new MainWindow();
                desktop.Exit += ( _, _ ) =>
                    UpdaterSettings.Save( UpdaterSettings, CurrentOptions?.Path ?? Environment.CurrentDirectory );
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
