using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Assistant;

namespace ClassicAssist.UI.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // InputLanguageManager removed (not available in Avalonia)
            // SystemParameters removed (not available in Avalonia)
        }

        // SystemDecorations="None" disables the OS-provided resize border, so the
        // bottom-right grip handles resize manually via BeginResizeDrag.
        private void ResizeGrip_PointerPressed( object sender, PointerPressedEventArgs e )
        {
            if ( !e.GetCurrentPoint( this ).Properties.IsLeftButtonPressed )
                return;

            BeginResizeDrag( WindowEdge.SouthEast, e );
        }
    }
}