using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace ClassicAssist.UI.Misc.ValueConverters
{
    // Returns true iff every binding has a value (non-null, non-UnsetValue) and,
    // when the value is bool, is true. Treating UnsetValue as falsy matches the
    // WPF MultiBinding semantics — an unresolved binding (e.g. SelectedItem.Foo
    // when SelectedItem is null) should disable the dependent control rather
    // than fall through as truthy.
    public class AllTrueMultiConverter : IMultiValueConverter
    {
        public object Convert( IList<object> values, System.Type targetType, object parameter, CultureInfo culture )
        {
            return values.All( v =>
            {
                if ( v == null ) return false;
                if ( v == Avalonia.AvaloniaProperty.UnsetValue ) return false;
                if ( v is bool b ) return b;
                return true;
            } );
        }
    }
}
