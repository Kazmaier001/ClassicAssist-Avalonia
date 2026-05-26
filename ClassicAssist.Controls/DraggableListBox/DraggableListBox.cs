#region License

// Copyright (C) 2023 Reetus
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
using System.Collections;
using Avalonia.Controls;
using Avalonia.Input;

namespace ClassicAssist.Controls.DraggableListBox
{
    public class DraggableListBox : ListBox
    {
        protected override Type StyleKeyOverride => typeof( ListBox );

        private object _draggedItem;
        private bool _isDragging;

        public DraggableListBox()
        {
            AddHandler( DragDrop.DropEvent, OnDrop );
            AddHandler( DragDrop.DragOverEvent, OnDragOver );
            DragDrop.SetAllowDrop( this, true );
        }

        protected override void OnPointerPressed( PointerPressedEventArgs e )
        {
            base.OnPointerPressed( e );

            var point = e.GetCurrentPoint( this );
            if ( !point.Properties.IsLeftButtonPressed )
                return;

            _draggedItem = (e.Source as Control)?.DataContext;
            _isDragging = _draggedItem != null;
        }

        protected override async void OnPointerMoved( PointerEventArgs e )
        {
            base.OnPointerMoved( e );

            if ( !_isDragging || _draggedItem == null )
                return;

            _isDragging = false;

            var data = new DataObject();
            data.Set( "DraggableItem", _draggedItem );

            await DragDrop.DoDragDrop( e, data, DragDropEffects.Move );
            _draggedItem = null;
        }

        private void OnDragOver( object sender, DragEventArgs e )
        {
            if ( !e.Data.Contains( "DraggableItem" ) )
            {
                e.DragEffects = DragDropEffects.None;
                return;
            }

            object sourceItem = e.Data.Get( "DraggableItem" );
            Type type = ItemsSource?.GetType().GetGenericArguments()[0];

            if ( type == null || sourceItem?.GetType() != type )
            {
                e.DragEffects = DragDropEffects.None;
                return;
            }

            e.DragEffects = DragDropEffects.Move;
        }

        private void OnDrop( object sender, DragEventArgs e )
        {
            if ( !e.Data.Contains( "DraggableItem" ) )
                return;

            object sourceItem = e.Data.Get( "DraggableItem" );
            Type type = ItemsSource?.GetType().GetGenericArguments()[0];

            if ( type == null || sourceItem?.GetType() != type )
                return;

            object targetItem = ( e.Source as Control )?.DataContext;

            object source = Convert.ChangeType( sourceItem, type );
            object target = targetItem != null ? Convert.ChangeType( targetItem, type ) : null;

            int sourceIndex = ( (IList) ItemsSource ).IndexOf( source );
            int targetIndex = target != null ? ( (IList) ItemsSource ).IndexOf( target ) : -1;

            ( (dynamic) ItemsSource ).Move( sourceIndex,
                targetIndex == -1 ? ( (IList) ItemsSource ).Count - 1 : targetIndex );
        }
    }
}