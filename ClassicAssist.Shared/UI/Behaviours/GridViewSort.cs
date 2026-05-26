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
using System.Collections;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.Shared.UI.Behaviours
{
    /// <summary>
    /// Sorting behaviour for DataGrid columns (Avalonia replacement for WPF GridViewSort).
    /// In Avalonia, ListView/GridView doesn't exist — use DataGrid instead.
    /// This behaviour attaches to a DataGrid and sorts by column header click.
    /// </summary>
    public class GridViewSort : Behavior<DataGrid>
    {
        public static readonly StyledProperty<Type> ComparerTypeProperty =
            AvaloniaProperty.Register<GridViewSort, Type>( nameof( ComparerType ) );

        private ListSortDirection _lastDirection;
        private DataGridColumn _lastColumnClicked;

        public Type ComparerType
        {
            get => GetValue( ComparerTypeProperty );
            set => SetValue( ComparerTypeProperty, value );
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Sorting += OnSorting;
        }

        private void OnSorting( object sender, DataGridColumnEventArgs e )
        {
            var column = e.Column;

            ListSortDirection direction;

            if ( !Equals( column, _lastColumnClicked ) )
            {
                direction = ListSortDirection.Ascending;
            }
            else
            {
                direction = _lastDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }

            // If a ComparerType is specified, use it; otherwise let Avalonia DataGrid handle default sorting
            if ( ComparerType != null )
            {
                string propertyName = column.Header?.ToString() ?? string.Empty;
                IComparer comparer = (IComparer) Activator.CreateInstance( ComparerType, direction, propertyName );

                // Avalonia DataGrid doesn't directly support CustomSort on ItemsSource.
                // For now, sort the source collection if it supports sorting.
                if ( AssociatedObject.ItemsSource is IList list )
                {
                    var sorted = list.Cast<object>().ToList();
                    sorted.Sort( (x, y) => comparer.Compare( x, y ) );
                    AssociatedObject.ItemsSource = sorted;
                }
            }

            _lastColumnClicked = column;
            _lastDirection = direction;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Sorting -= OnSorting;
            base.OnDetaching();
        }
    }
}