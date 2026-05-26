using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using ClassicAssist.UO.Data;

namespace ClassicAssist.UI.Controls
{
    /// <summary>
    ///     Interaction logic for HueEntryControl.xaml
    /// </summary>
    public partial class HueEntryControl : UserControl
    {
        public static readonly StyledProperty<HueEntry> HueEntryProperty =
            AvaloniaProperty.Register<HueEntryControl, HueEntry>( nameof( HueEntry ) );

        static HueEntryControl()
        {
            HueEntryProperty.Changed.AddClassHandler<HueEntryControl>( ( control, e ) =>
                control.SetHues( e.GetNewValue<HueEntry>() ) );
        }

        public HueEntryControl()
        {
            InitializeComponent();
        }

        public HueEntry HueEntry
        {
            get => GetValue( HueEntryProperty );
            set => SetValue( HueEntryProperty, value );
        }

        private void SetHues( HueEntry entry )
        {
            // ListBox virtualization and filter changes re-fire the property-changed callback
            // on the same control instance — clear prior swatches so they don't stack up.
            StackPanel.Children.Clear();

            if ( entry.Equals( default( HueEntry ) ) || entry.Colors == null ) return;

            for ( int i = 0; i < 32; i++ )
            {
                SolidColorBrush brush = new SolidColorBrush( Convert555ToARGB( entry.Colors[i] ) );

                StackPanel.Children.Add( new Rectangle { Fill = brush, Width = 10 } );
            }
        }

        protected static Color Convert555ToARGB( short Col )
        {
            int r = ( (short) ( Col >> 10 ) & 31 ) * 8;
            int g = ( (short) ( Col >> 5 ) & 31 ) * 8;
            int b = ( Col & 31 ) * 8;
            return Color.FromArgb( 255, (byte) r, (byte) g, (byte) b );
        }
    }
}