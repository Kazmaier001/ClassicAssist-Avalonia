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

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.Shared.UI.Behaviours
{
    public class ItemsControlAutoScrollBehaviour : Behavior<Control>
    {
        public static readonly StyledProperty<ScrollViewer> ScrollViewerProperty =
            AvaloniaProperty.Register<ItemsControlAutoScrollBehaviour, ScrollViewer>( nameof( ScrollViewer ) );

        private INotifyCollectionChanged _incc;

        public ScrollViewer ScrollViewer
        {
            get => GetValue( ScrollViewerProperty );
            set => SetValue( ScrollViewerProperty, value );
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            System.Collections.IEnumerable items = AssociatedObject switch
            {
                ItemsControl ic => ic.Items,
                DataGrid dg => dg.ItemsSource,
                _ => null
            };

            if ( items is INotifyCollectionChanged incc )
            {
                _incc = incc;
                _incc.CollectionChanged += OnCollectionChanged;
            }
        }

        private void OnCollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
        {
            ScrollViewer scrollViewer = ScrollViewer ?? AssociatedObject.GetChildOfType<ScrollViewer>();

            scrollViewer?.ScrollToEnd();
        }

        protected override void OnDetaching()
        {
            if ( _incc != null )
            {
                _incc.CollectionChanged -= OnCollectionChanged;
            }

            base.OnDetaching();
        }
    }
}