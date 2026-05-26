#region License

// Copyright (C) 2021 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassicAssist.Shared.UI.ValueConverters
{
    public class EnumToIntegerValueConverter : IValueConverter
    {
        // A constraint row's value column can host several editors (Layer ComboBox,
        // MultiItemID, free-text EditTextBlock, ...) all bound TwoWay to the same
        // model property. Only one is visible at a time but the bindings stay live.
        // When the visible editor commits an int that isn't a valid member of this
        // converter's enum (e.g. user types 35 into the free-text editor while a
        // Layer ComboBox is also bound), the ComboBox would otherwise pull the int,
        // fail to match a Layer member, drop SelectedItem to null, then echo null
        // back through ConvertBack(null) → 0, clobbering the user's value. Returning
        // DoNothing here keeps SelectedItem untouched on the source-pull so nothing
        // is echoed back.
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( !( parameter is Type type ) || value == null || value is UnsetValueType )
            {
                return BindingOperations.DoNothing;
            }

            try
            {
                int i = System.Convert.ToInt32( value, culture );
                // Convert to the underlying enum type via Enum.ToObject before calling
                // IsDefined — IsDefined throws if the raw int doesn't match the enum's
                // underlying type (e.g. Layer is byte-backed, passing Int32 throws
                // ArgumentException).
                object enumObj = Enum.ToObject( type, i );
                if ( Enum.IsDefined( type, enumObj ) )
                {
                    return enumObj;
                }
            }
            catch ( OverflowException ) { }
            catch ( InvalidCastException ) { }
            catch ( FormatException ) { }
            catch ( ArgumentException ) { }

            return BindingOperations.DoNothing;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value == null || value is UnsetValueType )
            {
                return BindingOperations.DoNothing;
            }

            return System.Convert.ToInt32( value );
        }
    }
}