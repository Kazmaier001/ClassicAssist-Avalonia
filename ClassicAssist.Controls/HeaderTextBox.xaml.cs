using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace ClassicAssist.Controls
{
    /// <summary>
    ///     Interaction logic for HeaderTextBox.axaml
    /// </summary>
    public partial class HeaderTextBox : UserControl
    {
        public static readonly StyledProperty<object> HeaderProperty =
            AvaloniaProperty.Register<HeaderTextBox, object>( nameof( Header ),
                defaultBindingMode: BindingMode.TwoWay );

        public static readonly StyledProperty<object> ValueProperty =
            AvaloniaProperty.Register<HeaderTextBox, object>( nameof( Value ), string.Empty,
                defaultBindingMode: BindingMode.TwoWay );

        public HeaderTextBox()
        {
            InitializeComponent();
        }

        public object Header
        {
            get => GetValue( HeaderProperty );
            set => SetValue( HeaderProperty, value );
        }

        public object Value
        {
            get => GetValue( ValueProperty );
            set => SetValue( ValueProperty, value );
        }

        private void TextBox_OnTextChanged( object sender, Avalonia.Controls.TextChangedEventArgs e )
        {
            if ( sender is TextBox textBox )
            {
                Value = textBox.Text;
            }
        }
    }
}