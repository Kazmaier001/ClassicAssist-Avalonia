#region License

// Copyright (C) 2026 Reetus
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
    // Apply to TwoWay bindings whose target can transiently hold null but whose source
    // is a non-nullable value type (e.g. ComboBox.SelectedItem bound to an enum). On
    // cell rebuilds the ComboBox can momentarily have no SelectedItem before its
    // ItemsSource is repopulated, and the binding would otherwise try to write null
    // back to the source and throw InvalidCastException.
    public class NullToDoNothingConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture ) =>
            value ?? BindingOperations.DoNothing;

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value == null || value is UnsetValueType )
            {
                return BindingOperations.DoNothing;
            }

            return value;
        }
    }
}
