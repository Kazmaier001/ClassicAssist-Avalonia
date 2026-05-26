using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using ClassicAssist.Shared.UI;

namespace ClassicAssist.Controls
{
    /// <summary>
    ///     Interaction logic for FilterControl.axaml
    /// </summary>
    public partial class FilterControl : UserControl
    {
        public static readonly StyledProperty<string> FilterTextProperty =
            AvaloniaProperty.Register<FilterControl, string>( nameof( FilterText ),
                defaultBindingMode: BindingMode.TwoWay );

        public static readonly StyledProperty<bool> IsFilterVisibleProperty =
            AvaloniaProperty.Register<FilterControl, bool>( nameof( IsFilterVisible ),
                defaultBindingMode: BindingMode.TwoWay );

        public static readonly StyledProperty<bool> ShowCloseButtonProperty =
            AvaloniaProperty.Register<FilterControl, bool>( nameof( ShowCloseButton ), true,
                defaultBindingMode: BindingMode.TwoWay );

        private ICommand _closeCommand;

        public FilterControl()
        {
            InitializeComponent();
        }

        public ICommand CloseCommand => _closeCommand ?? ( _closeCommand = new RelayCommand( Close ) );

        public string FilterText
        {
            get => GetValue( FilterTextProperty );
            set => SetValue( FilterTextProperty, value );
        }

        public bool IsFilterVisible
        {
            get => GetValue( IsFilterVisibleProperty );
            set => SetValue( IsFilterVisibleProperty, value );
        }

        public bool ShowCloseButton
        {
            get => GetValue( ShowCloseButtonProperty );
            set => SetValue( ShowCloseButtonProperty, value );
        }

        private void Close( object obj )
        {
            FilterText = string.Empty;
            IsFilterVisible = false;
        }
    }
}