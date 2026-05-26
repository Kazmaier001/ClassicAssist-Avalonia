#region License

// Copyright (C) 2022 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

#endregion

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace ClassicAssist.Shared.UI.ValueConverters
{
    /// <summary>
    /// Converts a boolean to its inverse for IsVisible binding.
    /// In Avalonia, Visibility enum doesn't exist - use bool with IsVisible instead.
    /// true -> false (hidden), false -> true (visible)
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( !( value is bool enabled ) )
            {
                return null;
            }

            // Avalonia uses bool for IsVisible, not Visibility enum
            // Inverse: enabled=true -> IsVisible=false, enabled=false -> IsVisible=true
            return !enabled;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return BindingOperations.DoNothing;
        }
    }
}