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
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using ClassicAssist.Data.Macros;
using ClassicAssist.Misc;

namespace ClassicAssist.UI.Views
{
    /// <summary>
    ///     Interaction logic for MacrosTabControl.xaml
    /// </summary>
    public partial class MacrosTabControl : UserControl
    {
        public MacrosTabControl()
        {
            InitializeComponent();

            // Avalonia BindingProxy doesn't inherit DataContext through Resources;
            // wire Proxy.Data explicitly so context-menu / popup commands
            // (NewGroup, RemoveGroup, MoveToGroup, ResetImportCache) resolve to the VM.
            DataContextChanged += ( s, e ) => SyncProxyData();
            SyncProxyData();
        }

        private void SyncProxyData()
        {
            if ( Resources.TryGetValue( "Proxy", out object proxyObj ) && proxyObj is BindingProxy proxy )
            {
                proxy.Data = DataContext;
            }
        }

        private void DraggableTreeView_OnPreviewMouseWheel( object sender, PointerWheelEventArgs e )
        {
            /*
             * Cheap hack for our broken template, no scrollbars, bubble event to parent scrollviewer
             */
            if ( !( sender is Control control ) || e.Handled )
            {
                return;
            }

            if ( control.Parent == null )
            {
                return;
            }

            e.Handled = true;
            // Forward wheel event to parent in Avalonia - let it bubble
            Control parent = control.Parent as Control;
            if ( parent != null )
            {
                e.Handled = false; // Let it bubble up naturally
            }
        }

        private void ToggleButton_OnIsCheckedChanged( object sender, Avalonia.Interactivity.RoutedEventArgs e )
        {
            if ( !( sender is Avalonia.Controls.Primitives.ToggleButton tb ) )
            {
                return;
            }

            var grid = tb.FindAncestorOfType<Grid>();
            while ( grid != null && grid.ColumnDefinitions.Count < 3 )
            {
                grid = grid.FindAncestorOfType<Grid>();
            }
            if ( grid != null && grid.ColumnDefinitions.Count > 0 )
            {
                grid.ColumnDefinitions[0].Width = tb.IsChecked == true ? new GridLength( 0 ) : new GridLength( 200 );
            }
        }

        internal class SameNameComparer : IEqualityComparer<PythonCompletionData>
        {
            public bool Equals( PythonCompletionData x, PythonCompletionData y )
            {
                return y != null && x != null && x.Name.Equals( y.Name );
            }

            public int GetHashCode( PythonCompletionData obj )
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}