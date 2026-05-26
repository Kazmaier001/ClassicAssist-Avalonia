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

using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using ClassicAssist.UI.Models;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.UI.Misc.Behaviours
{
    // Generic over any Control (was DataGrid in the WPF port). The Object
    // Inspector now uses a ListBox inside a grouped Expander; the double-click
    // handler walks from the event source up to the nearest item carrying an
    // ObjectInspectorData DataContext and invokes its OnDoubleClick.
    public class ListViewDoubleClickBehaviour : Behavior<Control>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            if ( AssociatedObject != null )
            {
                AssociatedObject.DoubleTapped += OnMouseDoubleClick;
            }
        }

        private static void OnMouseDoubleClick( object sender, Avalonia.Interactivity.RoutedEventArgs e )
        {
            // Walk up the visual tree from the click source to find a control
            // whose DataContext is an ObjectInspectorData.
            for ( Visual v = e.Source as Visual; v != null; v = v.GetVisualParent() )
            {
                if ( v is Control c && c.DataContext is ObjectInspectorData data )
                {
                    data.OnDoubleClick?.Invoke( data );
                    return;
                }
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if ( AssociatedObject != null )
            {
                AssociatedObject.DoubleTapped -= OnMouseDoubleClick;
            }
        }
    }
}