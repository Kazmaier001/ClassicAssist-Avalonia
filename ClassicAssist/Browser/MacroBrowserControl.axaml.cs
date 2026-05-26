using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace ClassicAssist.Browser
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MacroBrowserControl : UserControl
    {
        public MacroBrowserControl()
        {
            InitializeComponent();

            var codeEditor = this.FindControl<AvaloniaEdit.TextEditor>( "CodeTextEditor" );
            if ( codeEditor != null )
            {
                codeEditor.SyntaxHighlighting = HighlightingLoader.Load(
                    new XmlTextReader( Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ),
                        "Python.Dark.xshd" ) ), HighlightingManager.Instance );
            }
        }

        private void Hyperlink_RequestNavigate( object sender, Avalonia.Input.PointerReleasedEventArgs e )
        {
            if ( sender is Control control && control.Tag is string url )
            {
                Process.Start( new ProcessStartInfo( url ) { UseShellExecute = true } );
            }
        }

        // WPF used a DataTrigger on the ColumnDefinition.Width bound to the
        // ToggleButton's IsChecked to collapse the macros list column. Avalonia
        // doesn't have DataTriggers on ColumnDefinition.Width, so do it here.
        // Checked → list column collapses to 0; unchecked → restores 200 DIP.
        // GridSplitter and code-editor columns are unaffected, matching WPF.
        private void ToggleButton_IsCheckedChanged( object sender, Avalonia.Interactivity.RoutedEventArgs e )
        {
            if ( sender is not ToggleButton tb ) return;
            var grid = this.FindControl<Grid>( "SplitGrid" );
            if ( grid == null || grid.ColumnDefinitions.Count < 1 ) return;
            grid.ColumnDefinitions[0].Width = tb.IsChecked == true
                ? new GridLength( 0 )
                : new GridLength( 200 );
        }
    }
}