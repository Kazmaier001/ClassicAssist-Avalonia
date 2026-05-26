using Avalonia.Controls;
using Avalonia.Input;
using ClassicAssist.Data;
using ClassicAssist.UI.ViewModels;

namespace ClassicAssist.UI.Views
{
    /// <summary>
    ///     Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        public ChatWindow()
        {
            InitializeComponent();
            Width = Options.CurrentOptions.ChatWindowWidth;
            Height = Options.CurrentOptions.ChatWindowHeight;
            SizeChanged += OnSizeChanged;

            if ( DataContext is ChatViewModel vm )
            {
                vm.RightColumnSize = Options.CurrentOptions.ChatWindowRightColumn;
            }
        }

        private static void OnSizeChanged( object sender, SizeChangedEventArgs e )
        {
            Options.CurrentOptions.ChatWindowWidth = e.NewSize.Width;
            Options.CurrentOptions.ChatWindowHeight = e.NewSize.Height;
        }

        private void ResizeGrip_PointerPressed( object sender, PointerPressedEventArgs e )
        {
            if ( !e.GetCurrentPoint( this ).Properties.IsLeftButtonPressed )
                return;

            BeginResizeDrag( WindowEdge.SouthEast, e );
        }

        private void Thumb_OnDragCompleted( object sender, Avalonia.Input.VectorEventArgs e )
        {
            if ( DataContext is ChatViewModel vm )
            {
                Options.CurrentOptions.ChatWindowRightColumn = vm.RightColumnSize;
            }
        }
    }
}