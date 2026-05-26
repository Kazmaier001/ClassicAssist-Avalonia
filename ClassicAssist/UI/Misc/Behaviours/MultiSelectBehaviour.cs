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

using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.UI.Misc.Behaviours
{
    /*
     * https://stackoverflow.com/questions/8088595/synchronizing-multi-select-listbox-with-mvvm
     */

    // Was Behavior<DataGrid> in the WPF port; the only consumer
    // (EntityCollectionViewer) now uses a ListBox so this attaches to ListBox.
    // SelectingItemsControl.SelectedItems is protected; ListBox/DataGrid each
    // expose a public override — staying on ListBox avoids reflection.
    public class MultiSelectionBehaviour : Behavior<ListBox>
    {
        public static readonly StyledProperty<IList> SelectedItemsProperty = AvaloniaProperty.Register<MultiSelectionBehaviour, IList>( nameof( SelectedItems ) );

        static MultiSelectionBehaviour()
        {
            // Avalonia StyledProperty.Register doesn't wire change callbacks
            // automatically (per project [[avalonia-property-porting]]); without
            // this, SelectedItemsChanged never fires and the ListBox->VM sync
            // handler never gets attached, so VM.SelectedItems stays empty.
            SelectedItemsProperty.Changed.AddClassHandler<MultiSelectionBehaviour>(
                ( b, e ) => SelectedItemsChanged( b, e ) );
        }

        private bool _isUpdatingSource;

        private bool _isUpdatingTarget;

        public IList SelectedItems
        {
            get => (IList) GetValue( SelectedItemsProperty );
            set => SetValue( SelectedItemsProperty, value );
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if ( SelectedItems == null )
            {
                return;
            }

            AssociatedObject.SelectedItems.Clear();

            foreach ( object item in SelectedItems )
            {
                AssociatedObject.SelectedItems.Add( item );
            }

            // Bind both sync directions. SelectedItemsChanged only fires when
            // the SelectedItems StyledProperty itself is reassigned; if it was
            // already set BEFORE OnAttached (the common case — VM ctor creates
            // an ObservableCollection once and binds it), the handler wouldn't
            // pick that up. Wire up the live sync here so the first attach
            // covers it.
            AssociatedObject.SelectionChanged += ListBoxSelectionChanged;

            if ( SelectedItems is INotifyCollectionChanged incc )
            {
                incc.CollectionChanged += SourceCollectionChanged;
            }
        }

        protected override void OnDetaching()
        {
            if ( AssociatedObject != null )
            {
                AssociatedObject.SelectionChanged -= ListBoxSelectionChanged;
            }

            if ( SelectedItems is INotifyCollectionChanged incc )
            {
                incc.CollectionChanged -= SourceCollectionChanged;
            }

            base.OnDetaching();
        }

        private static void SelectedItemsChanged( AvaloniaObject o, AvaloniaPropertyChangedEventArgs e )
        {
            if ( o == null || !( o is MultiSelectionBehaviour behavior ) )
            {
                return;
            }

            INotifyCollectionChanged oldValue = (INotifyCollectionChanged) e.OldValue;
            INotifyCollectionChanged newValue = (INotifyCollectionChanged) e.NewValue;

            if ( oldValue != null )
            {
                oldValue.CollectionChanged -= behavior.SourceCollectionChanged;
                behavior.AssociatedObject.SelectionChanged -= behavior.ListBoxSelectionChanged;
            }

            if ( newValue == null || behavior.AssociatedObject == null )
            {
                return;
            }

            behavior.AssociatedObject.SelectedItems.Clear();

            if ( newValue is IEnumerable items )
            {
                foreach ( object item in items )
                {
                    behavior.AssociatedObject.SelectedItems.Add( item );
                }
            }

            behavior.AssociatedObject.SelectionChanged += behavior.ListBoxSelectionChanged;
            newValue.CollectionChanged += behavior.SourceCollectionChanged;
        }

        private void SourceCollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
        {
            if ( _isUpdatingSource )
            {
                return;
            }

            try
            {
                _isUpdatingTarget = true;

                if ( e.OldItems != null )
                {
                    foreach ( object item in e.OldItems )
                    {
                        AssociatedObject.SelectedItems.Remove( item );
                    }
                }

                if ( e.NewItems != null )
                {
                    foreach ( object item in e.NewItems )
                    {
                        AssociatedObject.SelectedItems.Add( item );
                    }
                }

                if ( e.Action == NotifyCollectionChangedAction.Reset )
                {
                    AssociatedObject.SelectedItems.Clear();
                }
            }
            finally
            {
                _isUpdatingTarget = false;
            }
        }

        private void ListBoxSelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( _isUpdatingTarget )
            {
                return;
            }

            IList selectedItems = SelectedItems;

            if ( selectedItems == null )
            {
                return;
            }

            try
            {
                _isUpdatingSource = true;

                foreach ( object item in e.RemovedItems )
                {
                    selectedItems.Remove( item );
                }

                foreach ( object item in e.AddedItems )
                {
                    selectedItems.Add( item );
                }
            }
            finally
            {
                _isUpdatingSource = false;
            }
        }
    }
}