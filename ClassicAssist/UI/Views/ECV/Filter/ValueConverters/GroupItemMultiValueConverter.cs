#region License

// Copyright (C) 2024 Reetus
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using ClassicAssist.UI.Views.ECV.Filter.Models;

namespace ClassicAssist.UI.Views.ECV.Filter.ValueConverters
{
    public class GroupItemMultiValueConverter : IMultiValueConverter
    {
        public object Convert( IList<object> values, Type targetType, object parameter, CultureInfo culture )
        {
            // Avalonia evaluates MultiBindings eagerly; sources may be UnsetValue
            // before all bindings settle. A blind cast here used to throw
            // InvalidCastException straight up the UI-thread and kill the plugin.
            if ( values == null || values.Count < 2 ||
                 values[0] is null || values[0] is UnsetValueType ||
                 values[1] is null || values[1] is UnsetValueType )
            {
                return BindingOperations.DoNothing;
            }

            if ( values[0] is not ObservableCollection<EntityCollectionFilterItem> group ||
                 values[1] is not EntityCollectionFilterItem item )
            {
                return BindingOperations.DoNothing;
            }

            return new GroupItem { Group = group, Item = item };
        }
    }
}