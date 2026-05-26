// Compatibility shim for WPF MessageBox -> Avalonia (backed by MsBox.Avalonia)
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace ClassicAssist.Misc
{
    public enum MessageBoxButton { OK, OKCancel, YesNo, YesNoCancel }
    public enum MessageBoxImage { None, Error, Warning, Information, Question }
    public enum MessageBoxResult { None, OK, Cancel, Yes, No }

    public static class MessageBox
    {
        public static MessageBoxResult Show( string message, string title = "",
            MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage image = MessageBoxImage.None )
        {
            // No desktop lifetime (TestHost smoke, headless tests, very early startup):
            // log and return the WPF default for the button set.
            if ( !( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime ) )
            {
                System.Diagnostics.Debug.WriteLine( $"[MessageBox] {title}: {message}" );
                return DefaultResult( button );
            }

            MessageBoxResult result = MessageBoxResult.None;

            if ( Dispatcher.UIThread.CheckAccess() )
            {
                result = ShowOnUIThread( message, title, button, image );
            }
            else
            {
                // Called from a worker (macro engine, network thread, etc.).
                // Marshal to UI, then block this thread until the dialog closes.
                using ( ManualResetEventSlim mre = new ManualResetEventSlim() )
                {
                    Dispatcher.UIThread.Post( () =>
                    {
                        try { result = ShowOnUIThread( message, title, button, image ); }
                        finally { mre.Set(); }
                    } );
                    mre.Wait();
                }
            }

            return result;
        }

        // Runs the modal on the UI thread and blocks the UI thread (without freezing it) via
        // a nested dispatcher MainLoop — the same trick HuePickerWindow.GetHue uses to keep
        // WPF-style synchronous ShowDialog semantics on top of Avalonia's async-only API.
        private static MessageBoxResult ShowOnUIThread( string message, string title,
            MessageBoxButton button, MessageBoxImage image )
        {
            var box = MessageBoxManager.GetMessageBoxStandard( title ?? string.Empty,
                message ?? string.Empty, MapButton( button ), MapImage( image ) );

            Window owner = null;
            if ( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                 && desktop.MainWindow != null && desktop.MainWindow.IsVisible )
            {
                owner = desktop.MainWindow;
            }

            Task<ButtonResult> task = owner != null
                ? box.ShowWindowDialogAsync( owner )
                : box.ShowWindowAsync();

            // MsBoxWindow doesn't pick up our global <Style Selector="Window"> icon (its
            // XAML pins something locally and local values beat style values in Avalonia).
            // Patch the freshly-opened MsBox window with our app icon directly.
            ApplyAppIcon();
            // Same trick: MsBox 3.3.x inlines VerticalAlignment="Stretch" on its
            // ContentTextBox via compiled XAML, defeating any Style setter we add.
            // Force-centre the text by writing directly to the local value.
            ApplyContentTextBoxAlignment();

            CancellationTokenSource cts = new CancellationTokenSource();
            task.ContinueWith( _ => cts.Cancel() );

            Dispatcher.UIThread.MainLoop( cts.Token );

            ButtonResult raw = task.IsCompletedSuccessfully ? task.Result : ButtonResult.None;
            return MapResult( raw, button );
        }

        private static void ApplyContentTextBoxAlignment()
        {
            if ( !( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ) )
            {
                return;
            }

            Window target = desktop.Windows.LastOrDefault( w => w.GetType().Name == "MsBoxWindow" );
            if ( target == null )
            {
                return;
            }

            // MsBox doesn't expose the TextBox publicly. Dispatch a tree walk after the
            // window's Loaded event so the template is realized.
            Dispatcher.UIThread.Post( () =>
            {
                DumpMsBoxTree( target );

                TextBox content = FindDescendant<TextBox>( target, "ContentTextBox" );
                if ( content == null )
                {
                    Console.Error.WriteLine( "[MessageBox] ContentTextBox NOT FOUND in MsBoxWindow tree." );
                    return;
                }
                // The TextBox sits in an Auto-sized row inside an inner Grid that's 50px
                // tall to match the icon column. VA on the TextBox itself can't help
                // because its row is exactly textbox-height. Set the PARENT Grid's VA so
                // the whole text block shrink-wraps and centers against the icon.
                Visual parent = content.GetVisualParent();
                if ( parent is Layoutable parentLayout )
                {
                    Console.Error.WriteLine( $"[MessageBox] Parent {parent.GetType().Name} BEFORE: VA={parentLayout.VerticalAlignment} Bounds={parent.Bounds}" );
                    parentLayout.VerticalAlignment = VerticalAlignment.Center;
                    Console.Error.WriteLine( $"[MessageBox] Parent {parent.GetType().Name} AFTER:  VA={parentLayout.VerticalAlignment} (relayout pending)" );
                }
                else
                {
                    Console.Error.WriteLine( $"[MessageBox] ContentTextBox visual parent is not Layoutable: {parent?.GetType().Name ?? "null"}" );
                }
            }, DispatcherPriority.Background );
        }

        private static void DumpMsBoxTree( Window root )
        {
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine( $"MsBoxWindow tree @ {DateTime.Now:HH:mm:ss.fff} Bounds={root.Bounds}" );
                WalkMsBox( root, 0, sb );
                string dir = System.IO.Path.GetDirectoryName( typeof( MessageBox ).Assembly.Location );
                string path = System.IO.Path.Combine( dir ?? System.IO.Path.GetTempPath(), "msbox-tree.log" );
                System.IO.File.WriteAllText( path, sb.ToString() );
                Console.Error.WriteLine( $"[MessageBox] tree dumped to {path}" );
            }
            catch ( Exception ex )
            {
                Console.Error.WriteLine( "[MessageBox] dump failed: " + ex );
            }
        }

        private static void WalkMsBox( Visual v, int depth, System.Text.StringBuilder sb )
        {
            string indent = new string( ' ', depth * 2 );
            string name = ( v as Control )?.Name;
            string nameStr = string.IsNullOrEmpty( name ) ? "" : $" Name='{name}'";
            string typeName = v.GetType().Name;
            string layout = "";
            if ( v is Layoutable l )
            {
                layout = $" Bounds={v.Bounds} VA={l.VerticalAlignment} HA={l.HorizontalAlignment} H={l.Height} MinH={l.MinHeight} Margin={l.Margin}";
            }
            if ( v is ContentControl cc )
            {
                layout += $" VCA={cc.VerticalContentAlignment} Padding={cc.Padding}";
            }
            if ( v is TextBox tx )
            {
                layout += $" VCA={tx.VerticalContentAlignment} Padding={tx.Padding} Text='{Trunc( tx.Text )}'";
            }
            if ( v is TextBlock tb )
            {
                layout += $" Text='{Trunc( tb.Text )}'";
            }
            sb.AppendLine( indent + typeName + nameStr + layout );
            foreach ( Visual child in v.GetVisualChildren() )
            {
                WalkMsBox( child, depth + 1, sb );
            }
        }

        private static string Trunc( string s ) => s == null ? "" : ( s.Length <= 40 ? s : s.Substring( 0, 40 ) + "…" );

        private static T FindDescendant<T>( Control root, string name ) where T : Control
        {
            foreach ( Control child in root.GetVisualDescendants().OfType<Control>() )
            {
                if ( child is T t && t.Name == name )
                {
                    return t;
                }
            }
            return null;
        }

        private static WindowIcon _cachedIcon;

        private static void ApplyAppIcon()
        {
            if ( !( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ) )
            {
                return;
            }

            Window target = desktop.Windows.LastOrDefault( w => w.GetType().Name == "MsBoxWindow" );
            if ( target == null )
            {
                return;
            }

            if ( _cachedIcon == null )
            {
                try
                {
                    using ( var s = AssetLoader.Open( new Uri( "avares://ClassicAssist/Resources/uo.ico" ) ) )
                        _cachedIcon = new WindowIcon( s );
                }
                catch
                {
                    return;
                }
            }

            target.Icon = _cachedIcon;
        }

        private static ButtonEnum MapButton( MessageBoxButton b )
        {
            switch ( b )
            {
                case MessageBoxButton.OKCancel: return ButtonEnum.OkCancel;
                case MessageBoxButton.YesNo: return ButtonEnum.YesNo;
                case MessageBoxButton.YesNoCancel: return ButtonEnum.YesNoCancel;
                default: return ButtonEnum.Ok;
            }
        }

        private static Icon MapImage( MessageBoxImage i )
        {
            switch ( i )
            {
                case MessageBoxImage.Error: return Icon.Error;
                case MessageBoxImage.Warning: return Icon.Warning;
                case MessageBoxImage.Information: return Icon.Info;
                case MessageBoxImage.Question: return Icon.Question;
                default: return Icon.None;
            }
        }

        private static MessageBoxResult MapResult( ButtonResult r, MessageBoxButton button )
        {
            switch ( r )
            {
                case ButtonResult.Ok: return MessageBoxResult.OK;
                case ButtonResult.Yes: return MessageBoxResult.Yes;
                case ButtonResult.No: return MessageBoxResult.No;
                case ButtonResult.Cancel:
                case ButtonResult.Abort: return MessageBoxResult.Cancel;
                default: return DefaultResult( button ); // closed via X — match WPF
            }
        }

        // Mirrors WPF's behavior when the user dismisses via the close box: returns the
        // "cancel" button for sets that have one, otherwise the affirmative button.
        private static MessageBoxResult DefaultResult( MessageBoxButton button )
        {
            switch ( button )
            {
                case MessageBoxButton.OKCancel: return MessageBoxResult.Cancel;
                case MessageBoxButton.YesNo: return MessageBoxResult.No;
                case MessageBoxButton.YesNoCancel: return MessageBoxResult.Cancel;
                default: return MessageBoxResult.OK;
            }
        }
    }

    public enum DialogResult { None, OK, Cancel, Yes, No, Abort, Retry, Ignore }
}
