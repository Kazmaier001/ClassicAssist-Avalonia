using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassicAssist.UI.Misc.ValueConverters
{
    public class DateTimeToTimeSpanValueConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return !( value is DateTime dt ) ? null : ( DateTime.Now - dt ).ToString();
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return BindingOperations.DoNothing;
        }
    }
}