using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ClassicAssist.UI.Misc.ValueConverters
{
    /// <summary>
    /// Converts a boolean value to IsVisible (true = visible, false = hidden).
    /// This replaces the WPF BooleanToVisibilityConverter for Avalonia where
    /// IsVisible is a bool property rather than a Visibility enum.
    /// </summary>
    public class BoolToIsVisibleConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is bool b )
            {
                return b;
            }

            return value != null;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is bool b )
            {
                return b;
            }

            return false;
        }
    }

    /// <summary>
    /// Inverse of BoolToIsVisibleConverter - true = hidden, false = visible.
    /// </summary>
    public class InverseBoolToIsVisibleConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is bool b )
            {
                return !b;
            }

            return value == null;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is bool b )
            {
                return !b;
            }

            return true;
        }
    }
}
