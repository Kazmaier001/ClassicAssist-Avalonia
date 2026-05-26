#region License

// Copyright (C) 2025 Reetus
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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Data.Converters;

namespace ClassicAssist.Shared.UI.ValueConverters
{
    // Avalonia's binding system doesn't consult [TypeConverter] the way WPF does, so enum values
    // decorated with [Description("...")] render as their member names instead of the description.
    // Use this converter explicitly in ItemTemplates / displayed bindings to recover the WPF look.
    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value == null )
            {
                return string.Empty;
            }

            FieldInfo field = value.GetType().GetField( value.ToString() );

            if ( field == null )
            {
                return value.ToString();
            }

            DescriptionAttribute attr = field.GetCustomAttributes( typeof( DescriptionAttribute ), false )
                .Cast<DescriptionAttribute>()
                .FirstOrDefault();

            return string.IsNullOrEmpty( attr?.Description ) ? value.ToString() : attr.Description;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture ) =>
            throw new NotSupportedException();
    }
}
