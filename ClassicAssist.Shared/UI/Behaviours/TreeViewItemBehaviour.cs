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

using Avalonia;
using Avalonia.Controls;

namespace ClassicAssist.Shared.UI.Behaviours
{
    //https://www.codeproject.com/Articles/28959/Introduction-to-Attached-Behaviors-in-WPF
    /// <summary>
    ///     Exposes attached behaviors that can be
    ///     applied to TreeViewItem objects.
    /// </summary>
    public static class TreeViewItemBehavior
    {
        #region IsBroughtIntoViewWhenSelected

        public static readonly AttachedProperty<bool> IsBroughtIntoViewWhenSelectedProperty =
            AvaloniaProperty.RegisterAttached<TreeViewItem, TreeViewItem, bool>(
                "IsBroughtIntoViewWhenSelected", false );

        public static bool GetIsBroughtIntoViewWhenSelected( TreeViewItem treeViewItem )
        {
            return treeViewItem.GetValue( IsBroughtIntoViewWhenSelectedProperty );
        }

        public static void SetIsBroughtIntoViewWhenSelected( TreeViewItem treeViewItem, bool value )
        {
            treeViewItem.SetValue( IsBroughtIntoViewWhenSelectedProperty, value );
        }

        static TreeViewItemBehavior()
        {
            IsBroughtIntoViewWhenSelectedProperty.Changed.AddClassHandler<TreeViewItem>(
                OnIsBroughtIntoViewWhenSelectedChanged );
        }

        private static void OnIsBroughtIntoViewWhenSelectedChanged( TreeViewItem item,
            AvaloniaPropertyChangedEventArgs e )
        {
            if ( e.NewValue is bool newVal )
            {
                if ( newVal )
                {
                    item.PropertyChanged += OnTreeViewItemPropertyChanged;
                }
                else
                {
                    item.PropertyChanged -= OnTreeViewItemPropertyChanged;
                }
            }
        }

        private static void OnTreeViewItemPropertyChanged( object sender, AvaloniaPropertyChangedEventArgs e )
        {
            if ( e.Property.Name != "IsSelected" )
            {
                return;
            }

            if ( e.NewValue is bool isSelected && isSelected && sender is TreeViewItem item )
            {
                item.BringIntoView();
            }
        }

        #endregion // IsBroughtIntoViewWhenSelected
    }
}