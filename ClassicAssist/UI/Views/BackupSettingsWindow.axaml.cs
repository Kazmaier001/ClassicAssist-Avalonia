using System;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ClassicAssist.UI.Views
{
    /// <summary>
    ///     Interaction logic for BackupSettingsWindow.xaml
    /// </summary>
    public partial class BackupSettingsWindow : Window
    {
        public BackupSettingsWindow()
        {
            InitializeComponent();
            Opened += ( s, e ) =>
            {
                Dispatcher.UIThread.Post( DumpTree, DispatcherPriority.Background );
            };
        }

        private void DumpTree()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine( $"BackupSettingsWindow visual tree dump @ {DateTime.Now:HH:mm:ss.fff}" );
                sb.AppendLine( $"Window Bounds={Bounds} ClientSize={ClientSize}" );
                Walk( this, 0, sb );

                string dir = Path.GetDirectoryName( typeof( BackupSettingsWindow ).Assembly.Location );
                string path = Path.Combine( dir ?? Path.GetTempPath(), "backup-settings-tree.log" );
                File.WriteAllText( path, sb.ToString() );
                Console.Error.WriteLine( $"[BackupSettingsWindow] tree dumped to {path}" );
            }
            catch ( Exception ex )
            {
                Console.Error.WriteLine( "[BackupSettingsWindow] dump failed: " + ex );
            }
        }

        private static void Walk( Visual v, int depth, StringBuilder sb )
        {
            string indent = new string( ' ', depth * 2 );
            string name = ( v as Control )?.Name;
            string nameStr = string.IsNullOrEmpty( name ) ? "" : $" Name='{name}'";
            string typeName = v.GetType().Name;

            string layout = "";
            if ( v is Layoutable l )
            {
                layout = $" Bounds={v.Bounds} VAlign={l.VerticalAlignment} HAlign={l.HorizontalAlignment} H={l.Height} MinH={l.MinHeight} MaxH={l.MaxHeight} Margin={l.Margin}";
            }
            if ( v is ContentControl cc )
            {
                layout += $" VCA={cc.VerticalContentAlignment} HCA={cc.HorizontalContentAlignment} Padding={cc.Padding}";
            }
            if ( v is TextBlock tb )
            {
                layout += $" Text='{Trunc( tb.Text )}'";
            }

            sb.AppendLine( indent + typeName + nameStr + layout );

            foreach ( Visual child in v.GetVisualChildren() )
            {
                Walk( child, depth + 1, sb );
            }
        }

        private static string Trunc( string s ) => s == null ? "" : ( s.Length <= 40 ? s : s.Substring( 0, 40 ) + "…" );
    }
}