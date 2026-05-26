using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace ClassicAssist.Controls
{
    /// <summary>
    ///     Interaction logic for EditTextBlock.axaml
    /// </summary>
    public partial class EditTextBlock : UserControl
    {
        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<EditTextBlock, string>( nameof( Text ),
                defaultBindingMode: BindingMode.TwoWay );

        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<EditTextBlock, string>( nameof( Label ) );

        public static readonly StyledProperty<bool> ShowIconProperty =
            AvaloniaProperty.Register<EditTextBlock, bool>( nameof( ShowIcon ) );

        public static readonly StyledProperty<bool> CanEditProperty =
            AvaloniaProperty.Register<EditTextBlock, bool>( nameof( CanEdit ), true );

        public static readonly StyledProperty<object> ButtonsProperty =
            AvaloniaProperty.Register<EditTextBlock, object>( nameof( Buttons ) );

        public EditTextBlock()
        {
            InitializeComponent();
        }

        public object Buttons
        {
            get => GetValue( ButtonsProperty );
            set => SetValue( ButtonsProperty, value );
        }

        public bool CanEdit
        {
            get => GetValue( CanEditProperty );
            set => SetValue( CanEditProperty, value );
        }

        public string Label
        {
            get => GetValue( LabelProperty );
            set => SetValue( LabelProperty, value );
        }

        public bool ShowIcon
        {
            get => GetValue( ShowIconProperty );
            set => SetValue( ShowIconProperty, value );
        }

        public string Text
        {
            get => GetValue( TextProperty );
            set => SetValue( TextProperty, value );
        }

        private void TextBlock_OnPointerPressed( object sender, PointerPressedEventArgs e )
        {
            if ( e.ClickCount <= 1 )
            {
                return;
            }

            if ( !CanEdit )
            {
                return;
            }

            ShowTextBox();
        }

        private void ShowTextBox()
        {
            // Seed the TextBox with the current Text. XAML-side ElementName binding
            // (TextBox.Text → root.Text) didn't reliably propagate writes back through
            // outer TwoWay chains (e.g. when the host binds Text to an int), so we sync
            // by hand on enter/exit of edit mode.
            textBox.Text = Text;

            textBlock.IsVisible = false;
            pencilButton.IsVisible = false;
            buttonsPanel.IsVisible = false;
            textBox.IsVisible = true;
            textBox.CaretIndex = textBox.Text?.Length ?? 0;
            textBox.SelectAll();

            Dispatcher.UIThread.Post( () => textBox.Focus() );
        }

        private void TextBox_OnKeyDown( object sender, KeyEventArgs e )
        {
            if ( e.Key == Key.Return )
            {
                TextBox_LostFocus( sender, null );
                return;
            }

            // When the EditTextBlock is hosted inside a ComboBox template (e.g. the
            // EditComboBox used for ECV filter profile names), KeyDown=Space would
            // bubble up to the ComboBox, whose class handler toggles the dropdown
            // and steals focus mid-type. Mark Space handled so the bubble stops at
            // the TextBox. Avalonia's TextBox processes Space via KeyDown itself
            // (not via the separate TextInput event), so marking Handled also kills
            // the character insertion — we have to insert the space manually.
            if ( e.Key == Key.Space )
            {
                int caret = textBox.CaretIndex;
                int selStart = textBox.SelectionStart;
                int selEnd = textBox.SelectionEnd;
                string text = textBox.Text ?? string.Empty;

                if ( selStart != selEnd )
                {
                    int start = System.Math.Min( selStart, selEnd );
                    int length = System.Math.Abs( selEnd - selStart );
                    text = text.Remove( start, length );
                    textBox.Text = text.Insert( start, " " );
                    textBox.CaretIndex = start + 1;
                    textBox.SelectionStart = textBox.SelectionEnd = start + 1;
                }
                else
                {
                    textBox.Text = text.Insert( caret, " " );
                    textBox.CaretIndex = caret + 1;
                }

                e.Handled = true;
            }
        }

        private void TextBox_LostFocus( object sender, RoutedEventArgs e )
        {
            // Commit the typed value back to Text via the StyledProperty setter so
            // outer bindings (and the TextBlock display MultiBinding) see the change.
            Text = textBox.Text;

            textBlock.IsVisible = true;
            pencilButton.IsVisible = ShowIcon;
            buttonsPanel.IsVisible = true;
            ( (TextBox) sender ).IsVisible = false;
        }

        private void Button_OnClick( object sender, RoutedEventArgs e )
        {
            ShowTextBox();
        }
    }
}