using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace ClassicAssist.Shared.UI.ValueConverters
{
    public class DoubleToGridLengthConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is double d && !double.IsNaN( d ) && d > 0 )
                return new GridLength( d, GridUnitType.Pixel );

            return new GridLength( 1, GridUnitType.Star );
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is GridLength g )
                return g.Value;

            return AvaloniaProperty.UnsetValue;
        }
    }
}
