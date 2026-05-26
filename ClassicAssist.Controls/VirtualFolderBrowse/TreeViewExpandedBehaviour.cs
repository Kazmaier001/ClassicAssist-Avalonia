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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.Controls.VirtualFolderBrowse
{
    public class TreeViewExpandedBehaviour : Behavior<TreeView>
    {
        public static readonly StyledProperty<ICommand> OnExpandedActionProperty =
            AvaloniaProperty.Register<TreeViewExpandedBehaviour, ICommand>( nameof( OnExpandedAction ) );

        public static readonly StyledProperty<object> SelectedItemProperty =
            AvaloniaProperty.Register<TreeViewExpandedBehaviour, object>( nameof( SelectedItem ),
                defaultBindingMode: BindingMode.TwoWay );

        public ICommand OnExpandedAction
        {
            get => GetValue( OnExpandedActionProperty );
            set => SetValue( OnExpandedActionProperty, value );
        }

        public object SelectedItem
        {
            get => GetValue( SelectedItemProperty );
            set => SetValue( SelectedItemProperty, value );
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += SelectionChanged;
        }

        private void SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( sender is TreeView treeView )
            {
                SelectedItem = treeView.SelectedItem;
            }

            // Check if the selected item is a TreeViewItem and if it was just expanded
            if ( e.AddedItems.Count > 0 )
            {
                var selectedItem = e.AddedItems[0];
                OnExpandedAction?.Execute( selectedItem );
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectionChanged -= SelectionChanged;
        }
    }
}