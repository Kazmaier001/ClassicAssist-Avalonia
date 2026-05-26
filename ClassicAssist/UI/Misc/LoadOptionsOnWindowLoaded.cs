using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Assistant;
using ClassicAssist.Data;
using ClassicAssist.Shared;
using Sentry;

namespace ClassicAssist.UI.Misc
{
    public class LoadOptionsOnWindowLoaded : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.Closing += OnClosing;
        }

        private static void OnClosing( object sender, CancelEventArgs e )
        {
            // Match WPF ClassicAssist: the assistant window can't be closed independently of
            // ClassicUO. If the user clicks the X, cancel — the window only closes when
            // Engine.OnClientClosing (CUO's shutdown callback) flips IsShuttingDown and asks
            // the Avalonia lifetime to shut down. Saves are handled by OnClientClosing on
            // the host shutdown path, so we don't need to Save here.
            if ( !Engine.IsShuttingDown )
            {
                e.Cancel = true;
            }
        }

        private static void OnLoaded( object sender, RoutedEventArgs e )
        {
            // Profile load is a tree of synchronous file I/O + Deserialize calls into the
            // VM instances; running it here on the UI thread before first paint hangs the
            // window. Tried Task.Run — Deserialize touches StyledProperty/UI properties
            // from the worker thread and the exception path closes the window immediately.
            // Correct approach: load the profile in the bootstrap path BEFORE MainWindow is
            // constructed (see TestHost.Program.BootstrapEngineStubs). For the plugin entry
            // point (Engine.Install) the same applies.
            // Settings.Default removed (WPF-specific)
            {
                return;
            }

            SentryOptions options = new SentryOptions
            {
            // Settings.Default removed (WPF-specific)
                AutoSessionTracking = true,
                Release = VersionHelpers.GetProductVersion( Assembly.GetExecutingAssembly() ).ToString()
            };

            options.SetBeforeSend( SentryBeforeSend );
            SentrySdk.Init( options );
        }

        private static SentryEvent SentryBeforeSend( SentryEvent args )
        {
            if ( args.Exception is AggregateException && args.Exception.InnerException is SocketException )
            {
                return null;
            }

            if ( args.Exception?.TargetSite?.Module.Assembly == Engine.ClassicAssembly ||
                 ( args.Exception?.TargetSite?.Module.Assembly.ToString().Contains( "FNA" ) ?? false ) )
            {
                return null;
            }
#if DEBUG
            return null;
#else
            args.User = new SentryUser { Id = AssistantOptions.UserId };
            args.SetTag( "SessionId", AssistantOptions.SessionId );
            args.SetExtra( "PlayerName", Engine.Player?.Name ?? "Unknown" );
            args.SetExtra( "PlayerSerial", Engine.Player?.Serial ?? 0 );
            args.SetExtra( "Shard", Engine.CurrentShard?.Name ?? "Unknown" );
            args.SetExtra( "ShardFeatures", Engine.Features.ToString() );
            args.SetExtra( "CharacterListFlags", Engine.CharacterListFlags.ToString() );
            args.SetExtra( "Connected", Engine.Connected );
            args.SetExtra( "ClientVersion",
                Engine.ClientVersion == null ? "Unknown" : Engine.ClientVersion.ToString() );
            args.SetExtra( "ClassicUO Version", Engine.ClassicAssembly?.GetName().Version.ToString() ?? "Unknown" );

            return args;
#endif
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.Closing -= OnClosing;
            base.OnDetaching();
        }
    }
}