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
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.UI.Misc.Behaviours
{
    public class BindableSelectedItemBehavior : Behavior<TreeView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.SelectionChanged += OnTreeViewSelectedItemChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if ( AssociatedObject != null )
            {
                AssociatedObject.SelectionChanged -= OnTreeViewSelectedItemChanged;
            }
        }

        private void OnTreeViewSelectedItemChanged( object sender, SelectionChangedEventArgs e )
        {
            SelectedItem = e.AddedItems.Count > 0 ? e.AddedItems[0] : null;
        }

        #region SelectedItem Property

        public object SelectedItem
        {
            get => GetValue( SelectedItemProperty );
            set => SetValue( SelectedItemProperty, value );
        }

        public static readonly StyledProperty<object> SelectedItemProperty = AvaloniaProperty.Register<BindableSelectedItemBehavior, object>( "SelectedItem" );

        private static void OnSelectedItemChanged( AvaloniaObject sender, AvaloniaPropertyChangedEventArgs e )
        {
            if ( e.NewValue is TreeViewItem item )
            {
                item.SetValue( TreeViewItem.IsSelectedProperty, true );
            }
        }

        #endregion
    }
}