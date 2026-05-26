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
using Avalonia.Data.Converters;
using ClassicAssist.UI.Views.ECV.Filter.Models;

namespace ClassicAssist.UI.Views.ECV.Filter.ValueConverters
{
    public class CollectionIndexZeroVisibilityConverter : IMultiValueConverter
    {
        public object Convert( IList<object> values, Type targetType, object parameter, CultureInfo culture )
        {
            if ( !( values[0] is ObservableCollection<EntityCollectionFilterGroup> collection ) ||
                 !( values[1] is EntityCollectionFilterGroup item ) )
            {
                return false;
            }

            int index = collection.IndexOf( item );

            return index == 0 ? false : true;
        }

        
    }
}