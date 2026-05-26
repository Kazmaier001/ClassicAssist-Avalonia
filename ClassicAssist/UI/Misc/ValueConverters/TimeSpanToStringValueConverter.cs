using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassicAssist.UI.Misc.ValueConverters
{
    public class TimeSpanToStringValueConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is TimeSpan ts )
            {
                return ts.ToString( @"hh\:mm\:ss", CultureInfo.InvariantCulture );
            }

            return string.Empty;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is string s &&
                 TimeSpan.TryParseExact( s, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out TimeSpan parsed ) )
            {
                return parsed;
            }

            return BindingOperations.DoNothing;
        }
    }
}
