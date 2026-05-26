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
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassicAssist.Shared.UI.ValueConverters
{
    // Use this explicitly when a TwoWay binding crosses string ↔ int boundaries
    // (e.g. EditTextBlock.Text bound to an int data-model property). Avalonia's
    // default binding-level conversion will silently swallow ConvertBack failures
    // and leave the source untouched, so without an explicit converter the user's
    // typed value never reaches the model.
    public class StringToIntConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture ) =>
            value?.ToString() ?? string.Empty;

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is string s && int.TryParse( s, NumberStyles.Integer, culture, out int parsed ) )
            {
                return parsed;
            }

            // Keep the old source value on bad input rather than zeroing it out.
            return BindingOperations.DoNothing;
        }
    }
}
