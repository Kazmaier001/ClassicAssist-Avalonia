using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Windows.Input;
using ClassicAssist.Shared.UI;

namespace ClassicAssist.UI.Controls
{
    /// <summary>
    ///     Interaction logic for CustomWindowTitleControl.xaml
    /// </summary>
    public partial class CustomWindowTitleControl : UserControl
    {
        public static readonly StyledProperty<object> AdditionalContentProperty =
            AvaloniaProperty.Register<CustomWindowTitleControl, object>( nameof( AdditionalContent ) );

        public static readonly StyledProperty<object> AdditionalButtonsProperty =
            AvaloniaProperty.Register<CustomWindowTitleControl, object>( nameof( AdditionalButtons ) );

        public static readonly StyledProperty<string> CustomTitleProperty =
            AvaloniaProperty.Register<CustomWindowTitleControl, string>( nameof( CustomTitle ) );

        public static readonly StyledProperty<bool> CanCloseProperty =
            AvaloniaProperty.Register<CustomWindowTitleControl, bool>( nameof( CanClose ), defaultValue: true );

        public static readonly StyledProperty<bool> CanMinimizeProperty =
            AvaloniaProperty.Register<CustomWindowTitleControl, bool>( nameof( CanMinimize ), defaultValue: true );

        public static readonly StyledProperty<bool> CanMaxmizeProperty =
            AvaloniaProperty.Register<CustomWindowTitleControl, bool>( nameof( CanMaximize ), defaultValue: true );

        public static readonly StyledProperty<ICommand> MinimizeCommandProperty =
            AvaloniaProperty.Register<CustomWindowTitleControl, ICommand>( nameof( MinimizeCommand ) );

        private ICommand _maximizeCommand;

        public CustomWindowTitleControl()
        {
            InitializeComponent();
            DoubleTapped += OnDoubleTapped;
        }

        private void OnDoubleTapped( object sender, TappedEventArgs e )
        {
            if ( !CanMaximize )
                return;

            if ( !( TopLevel.GetTopLevel( this ) is Window window ) )
                return;

            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        public object AdditionalButtons
        {
            get => GetValue( AdditionalButtonsProperty );
            set => SetValue( AdditionalButtonsProperty, value );
        }

        public object AdditionalContent
        {
            get => GetValue( AdditionalContentProperty );
            set => SetValue( AdditionalContentProperty, value );
        }

        public bool CanClose
        {
            get => GetValue( CanCloseProperty );
            set => SetValue( CanCloseProperty, value );
        }

        public bool CanMaximize
        {
            get => GetValue( CanMaxmizeProperty );
            set => SetValue( CanMaxmizeProperty, value );
        }

        public bool CanMinimize
        {
            get => GetValue( CanMinimizeProperty );
            set => SetValue( CanMinimizeProperty, value );
        }

        public string CustomTitle
        {
            get => GetValue( CustomTitleProperty );
            set => SetValue( CustomTitleProperty, value );
        }

        public ICommand MaximizeCommand =>
            _maximizeCommand ?? ( _maximizeCommand = new RelayCommand( Maximize, o => true ) );

        public ICommand MinimizeCommand
        {
            get => GetValue( MinimizeCommandProperty );
            set => SetValue( MinimizeCommandProperty, value );
        }

        private static void Maximize( object obj )
        {
            if ( !( obj is Control element ) )
            {
                return;
            }

            Window window = TopLevel.GetTopLevel( element ) as Window;

            if ( window == null )
            {
                return;
            }

            window.WindowState =
                window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}