using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassicAssist.UI.Misc.ValueConverters
{
    // NumericUpDown.Value is decimal?, so binding to an int property fails to
    // round-trip when the user clears the field — Avalonia's automatic
    // null → int conversion throws InvalidCastException and DataValidationErrors
    // surfaces it inline.  Use this converter to swallow the null write back
    // (leaves the int property unchanged) and to widen ints into decimal? for
    // display.
    public class NullableDecimalToIntValueConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return value switch
            {
                int i => (decimal) i,
                decimal d => d,
                _ => null
            };
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return value switch
            {
                decimal d => (int) d,
                int i => i,
                _ => BindingOperations.DoNothing
            };
        }
    }
}
