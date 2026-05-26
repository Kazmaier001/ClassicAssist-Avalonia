using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ClassicAssist.Controls.DraggableTreeView
{
    /*
     * Credit: https://www.codeproject.com/Articles/55168/Drag-and-Drop-Feature-in-WPF-TreeView-Control
     * Simplified Avalonia port without adorners (Option B).
     */
    public class DraggableTreeView : TreeView
    {
        protected override Type StyleKeyOverride => typeof( TreeView );

        static DraggableTreeView()
        {
            // VM → TreeView selection propagation. Without this the bound
            // SelectedItem (e.g. loaded from the profile) never lights up the
            // TreeViewItem container — there is no "selected but unfocused"
            // grey state on first load because no item is actually selected.
            BindableSelectedItemProperty.Changed.AddClassHandler<DraggableTreeView>(
                ( tv, e ) => tv.OnBindableSelectedItemChanged( e.NewValue ) );
        }

        public static readonly StyledProperty<IDraggableEntry> BindableSelectedItemProperty =
            AvaloniaProperty.Register<DraggableTreeView, IDraggableEntry>( nameof( BindableSelectedItem ),
                defaultBindingMode: BindingMode.TwoWay );

        public static readonly StyledProperty<IDraggableGroup> BindableSelectedGroupProperty =
            AvaloniaProperty.Register<DraggableTreeView, IDraggableGroup>( nameof( BindableSelectedGroup ),
                defaultBindingMode: BindingMode.TwoWay );

        public static readonly StyledProperty<bool> AllowDragGroupsProperty =
            AvaloniaProperty.Register<DraggableTreeView, bool>( nameof( AllowDragGroups ), true );

        public static readonly StyledProperty<bool> AllowDragItemsOntoItemsProperty =
            AvaloniaProperty.Register<DraggableTreeView, bool>( nameof( AllowDragItemsOntoItems ), true );

        private object _draggedItem;
        private bool _isDragging;
        private Point _pressedPoint;
        private const double DragThreshold = 4;
        private TreeViewItem _dropTarget;

        public DraggableTreeView()
        {
            AddHandler( DragDrop.DropEvent, OnDrop );
            AddHandler( DragDrop.DragOverEvent, OnDragOver );
            AddHandler( DragDrop.DragLeaveEvent, OnDragLeave );
            DragDrop.SetAllowDrop( this, true );

            SelectionChanged += OnSelectionChanged;

            // Late-arriving containers: when an item container is created
            // after the bound SelectedItem was set (the typical first-load
            // case), apply IsSelected here.
            ContainerPrepared += OnContainerPrepared;
        }

        protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
        {
            base.OnPropertyChanged( change );

            // Drive a custom ":tree-focused" pseudoclass off IsKeyboardFocusWithin
            // — toggled true while the TreeView (or any descendant) holds keyboard
            // focus. Selectors use this to render blue "active" selection vs grey
            // "inactive" selection (focus moved elsewhere).
            if ( change.Property == IsKeyboardFocusWithinProperty )
            {
                PseudoClasses.Set( ":tree-focused", change.GetNewValue<bool>() );
            }
        }

        private void OnContainerPrepared( object sender, ContainerPreparedEventArgs e )
        {
            if ( BindableSelectedItem != null && e.Container is TreeViewItem tvi &&
                 ReferenceEquals( tvi.DataContext, BindableSelectedItem ) )
            {
                tvi.IsSelected = true;
            }
        }

        private void OnBindableSelectedItemChanged( object newValue )
        {
            // Route through the base TreeView's canonical SelectedItem so it
            // auto-deselects the previously-selected TreeViewItem. Setting only
            // the new container's IsSelected = true leaves the old one visually
            // selected too (double-highlight) — observed when the VM creates a
            // new macro and assigns it as SelectedItem from code.
            if ( !ReferenceEquals( SelectedItem, newValue ) )
            {
                SelectedItem = newValue;
            }

            // Safety net: walk realized containers and clear any stale
            // IsSelected that the base SelectedItem update may have missed
            // (e.g. when ItemsSource was just swapped and old containers
            // haven't been recycled yet).
            foreach ( var item in this.GetVisualDescendants().OfType<TreeViewItem>() )
            {
                if ( item.IsSelected && !ReferenceEquals( item.DataContext, newValue ) )
                {
                    item.IsSelected = false;
                }
            }

            if ( newValue == null )
                return;

            if ( ContainerFromItem( newValue ) is TreeViewItem tvi )
            {
                tvi.IsSelected = true;
            }
            // If container isn't generated yet, OnContainerPrepared will
            // catch it when the realization happens.
        }


        public bool AllowDragGroups
        {
            get => GetValue( AllowDragGroupsProperty );
            set => SetValue( AllowDragGroupsProperty, value );
        }

        public bool AllowDragItemsOntoItems
        {
            get => GetValue( AllowDragItemsOntoItemsProperty );
            set => SetValue( AllowDragItemsOntoItemsProperty, value );
        }

        public IDraggableGroup BindableSelectedGroup
        {
            get => GetValue( BindableSelectedGroupProperty );
            set => SetValue( BindableSelectedGroupProperty, value );
        }

        public IDraggableEntry BindableSelectedItem
        {
            get => GetValue( BindableSelectedItemProperty );
            set => SetValue( BindableSelectedItemProperty, value );
        }

        protected override void OnPointerPressed( PointerPressedEventArgs e )
        {
            base.OnPointerPressed( e );

            var point = e.GetCurrentPoint( this );
            if ( !point.Properties.IsLeftButtonPressed )
                return;

            _draggedItem = ( e.Source as Control )?.DataContext;
            _isDragging = _draggedItem is IDraggable;
            _pressedPoint = point.Position;
        }

        protected override void OnPointerReleased( PointerReleasedEventArgs e )
        {
            base.OnPointerReleased( e );

            // A click without a drag must clear the pending state, otherwise
            // any subsequent pointer move (even with the button up) would
            // start a drag-drop session — which captures the pointer, moves
            // keyboard focus off the tree and visibly steals the selection
            // highlight.
            _isDragging = false;
            _draggedItem = null;
        }

        protected override async void OnPointerMoved( PointerEventArgs e )
        {
            base.OnPointerMoved( e );

            if ( !_isDragging || _draggedItem == null )
                return;

            var point = e.GetCurrentPoint( this );
            if ( !point.Properties.IsLeftButtonPressed )
            {
                _isDragging = false;
                _draggedItem = null;
                return;
            }

            // Wait until we've moved past the drag threshold before starting
            // a drag-drop session — otherwise a normal click (which usually
            // jitters a pixel or two) triggers DoDragDrop and steals focus.
            var delta = point.Position - _pressedPoint;
            if ( Math.Abs( delta.X ) < DragThreshold && Math.Abs( delta.Y ) < DragThreshold )
                return;

            _isDragging = false;

            var data = new DataObject();
            data.Set( "DraggableItem", _draggedItem );

            await DragDrop.DoDragDrop( e, data, DragDropEffects.Move );
            _draggedItem = null;

            // DragDrop captures the pointer and moves keyboard focus off the
            // tree. Without this, the just-dropped macro flips to the grey
            // inactive-selection look the instant the drop completes — WPF
            // kept it blue. Restore focus to the active container.
            if ( ContainerFromItem( BindableSelectedItem ) is TreeViewItem tvi )
            {
                tvi.Focus();
            }
            else
            {
                Focus();
            }
        }

        private void OnDragOver( object sender, DragEventArgs e )
        {
            if ( !e.Data.Contains( "DraggableItem" ) )
            {
                e.DragEffects = DragDropEffects.None;
                ClearDropTarget();
                return;
            }

            var sourceItem = e.Data.Get( "DraggableItem" ) as IDraggable;
            if ( sourceItem == null )
            {
                e.DragEffects = DragDropEffects.None;
                ClearDropTarget();
                return;
            }

            if ( !( ItemsSource is ObservableCollection<IDraggable> items ) )
            {
                e.DragEffects = DragDropEffects.None;
                ClearDropTarget();
                return;
            }

            var targetItem = ( e.Source as Control )?.DataContext;
            ObservableCollection<IDraggable> sourceParent = GetParent( sourceItem, items );

            if ( !AllowDragItemsOntoItems && sourceItem is IDraggableEntry && sourceParent == items &&
                 !( targetItem is IDraggableGroup ) )
            {
                e.DragEffects = DragDropEffects.None;
                ClearDropTarget();
                return;
            }

            e.DragEffects = DragDropEffects.Move;
            UpdateDropTarget( e.Source as Visual, sourceItem );
        }

        private void OnDragLeave( object sender, DragEventArgs e )
        {
            ClearDropTarget();
        }

        private void UpdateDropTarget( Visual source, IDraggable sourceItem )
        {
            var tvi = FindAncestorTreeViewItem( source );

            // Don't decorate the dragged item itself, or anything that isn't
            // a valid drop target.
            if ( tvi == null || ReferenceEquals( tvi.DataContext, sourceItem ) )
            {
                ClearDropTarget();
                return;
            }

            if ( ReferenceEquals( tvi, _dropTarget ) )
                return;

            if ( _dropTarget != null )
                ( (IPseudoClasses) _dropTarget.Classes ).Set( ":drag-over", false );

            _dropTarget = tvi;
            ( (IPseudoClasses) _dropTarget.Classes ).Set( ":drag-over", true );
        }

        private void ClearDropTarget()
        {
            if ( _dropTarget == null )
                return;

            ( (IPseudoClasses) _dropTarget.Classes ).Set( ":drag-over", false );
            _dropTarget = null;
        }

        private static TreeViewItem FindAncestorTreeViewItem( Visual v )
        {
            while ( v != null )
            {
                if ( v is TreeViewItem tvi )
                    return tvi;
                v = v.GetVisualParent();
            }
            return null;
        }

        private void OnDrop( object sender, DragEventArgs e )
        {
            ClearDropTarget();

            if ( !e.Data.Contains( "DraggableItem" ) )
                return;

            var sourceItem = e.Data.Get( "DraggableItem" ) as IDraggable;
            if ( sourceItem == null )
                return;

            var targetItem = ( e.Source as Control )?.DataContext;

            switch ( targetItem )
            {
                case IDraggableGroup destinationGroup:
                {
                    ObservableCollection<IDraggable> parent =
                        GetParent( sourceItem, ItemsSource as ObservableCollection<IDraggable> );

                    switch ( sourceItem )
                    {
                        case IDraggableGroup _ when !AllowDragGroups:
                        case IDraggableGroup draggableGroup when IsParentOf( destinationGroup, draggableGroup ):
                            return;
                    }

                    parent?.Remove( sourceItem );
                    destinationGroup.Children.Add( sourceItem );
                    break;
                }
                case IDraggableEntry destinationItem:
                {
                    if ( !( ItemsSource is ObservableCollection<IDraggable> items ) )
                        return;

                    ObservableCollection<IDraggable> sourceParent = GetParent( sourceItem, items );

                    if ( !AllowDragItemsOntoItems && sourceItem is IDraggableEntry && sourceParent == items )
                        return;

                    if ( sourceItem is IDraggableGroup )
                        return;

                    ObservableCollection<IDraggable> destinationParent =
                        GetParent( destinationItem, ItemsSource as ObservableCollection<IDraggable> );

                    if ( sourceParent != destinationParent )
                    {
                        sourceParent?.Remove( sourceItem );
                        destinationParent?.Add( sourceItem );
                    }

                    if ( destinationParent != null )
                    {
                        int sourceIndex = destinationParent.IndexOf( sourceItem );
                        int targetIndex = destinationParent.IndexOf( destinationItem );
                        destinationParent.Move( sourceIndex, targetIndex );
                    }

                    break;
                }
                case null:
                {
                    if ( !( ItemsSource is ObservableCollection<IDraggable> items ) )
                        return;

                    ObservableCollection<IDraggable> sourceParent = GetParent( sourceItem, items );
                    sourceParent?.Remove( sourceItem );
                    items.Add( sourceItem );
                    break;
                }
            }
        }

        private void OnSelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            var selectedItem = SelectedItem;

            switch ( selectedItem )
            {
                case IDraggableGroup draggableGroup:
                    BindableSelectedGroup = draggableGroup;
                    BindableSelectedItem = null;
                    break;
                case IDraggableEntry draggableEntry:
                    BindableSelectedItem = draggableEntry;
                    BindableSelectedGroup = null;
                    break;
            }
        }

        private static bool IsParentOf( IDraggable sourceItem, IDraggableGroup destinationGroup )
        {
            return destinationGroup.Children.Any( e => e == sourceItem ) || GetGroups( destinationGroup.Children )
                .Any( draggableGroup => IsParentOf( sourceItem, draggableGroup ) );
        }

        private static IEnumerable<IDraggableGroup> GetGroups( IEnumerable<IDraggable> collection )
        {
            return collection.Where( i => i is IDraggableGroup ).Cast<IDraggableGroup>();
        }

        private static ObservableCollection<IDraggable> GetParent( IDraggable draggable,
            ObservableCollection<IDraggable> parent )
        {
            if ( parent == null )
                return null;

            if ( parent.Contains( draggable ) )
                return parent;

            IEnumerable<IDraggableGroup> groups = GetGroups( parent );

            return groups.Select( draggableGroup => GetParent( draggable, draggableGroup.Children ) )
                .FirstOrDefault( childParent => childParent != null );
        }
    }
}