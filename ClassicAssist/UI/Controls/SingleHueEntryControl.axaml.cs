using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ClassicAssist.UO.Data;

namespace ClassicAssist.UI.Controls
{
    /// <summary>
    ///     Single-swatch representation of a HueEntry. Companion to <see cref="HueEntryControl"/>
    ///     which renders the full 32-step gradient.
    /// </summary>
    public partial class SingleHueEntryControl : UserControl
    {
        public static readonly StyledProperty<HueEntry> HueEntryProperty =
            AvaloniaProperty.Register<SingleHueEntryControl, HueEntry>( nameof( HueEntry ) );

        static SingleHueEntryControl()
        {
            HueEntryProperty.Changed.AddClassHandler<SingleHueEntryControl>( ( control, e ) =>
                control.SetHue( e.GetNewValue<HueEntry>() ) );
        }

        public SingleHueEntryControl()
        {
            InitializeComponent();
        }

        public HueEntry HueEntry
        {
            get => GetValue( HueEntryProperty );
            set => SetValue( HueEntryProperty, value );
        }

        private void SetHue( HueEntry entry )
        {
            // Hue tables hold 32 ARGB1555 entries. Render them as a horizontal
            // gradient brush across the swatch so the picker row previews the
            // FULL hue (matches what HueEntryControl draws as 32 rectangles).
            if ( entry.Colors == null || entry.Colors.Length == 0 )
            {
                HueRectangle.Fill = Brushes.Black;
                return;
            }

            LinearGradientBrush gradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint( 0, 0, RelativeUnit.Relative ),
                EndPoint = new RelativePoint( 1, 0, RelativeUnit.Relative )
            };

            int last = entry.Colors.Length - 1;

            for ( int i = 0; i <= last; i++ )
            {
                gradient.GradientStops.Add( new GradientStop
                {
                    Color = Convert555ToARGB( entry.Colors[i] ),
                    Offset = (double) i / last
                } );
            }

            HueRectangle.Fill = gradient;
        }

        protected static Color Convert555ToARGB( short col )
        {
            int r = ( ( col >> 10 ) & 31 ) * 8;
            int g = ( ( col >> 5 ) & 31 ) * 8;
            int b = ( col & 31 ) * 8;
            return Color.FromArgb( 255, (byte) r, (byte) g, (byte) b );
        }
    }
}
