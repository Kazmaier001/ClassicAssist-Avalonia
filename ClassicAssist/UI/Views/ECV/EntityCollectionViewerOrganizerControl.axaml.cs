// Copyright (C) 2024 Reetus
//  
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

using Avalonia;
using Avalonia.Controls;
using ClassicAssist.Shared.UI;
using ClassicAssist.UI.ViewModels;
using ClassicAssist.UO.Objects;
using ItemCollection = ClassicAssist.UO.Objects.ItemCollection;

namespace ClassicAssist.UI.Views.ECV
{
    /// <summary>
    ///     Interaction logic for EntityCollectionViewerOrganizerControl.xaml
    /// </summary>
    public partial class EntityCollectionViewerOrganizerControl : UserControl
    {
        public static readonly StyledProperty<ItemCollection> CollectionProperty = AvaloniaProperty.Register<EntityCollectionViewerOrganizerControl, ItemCollection>( nameof( Collection ) );

        public static readonly StyledProperty<ObservableCollectionEx<QueueAction>> QueueActionsProperty = AvaloniaProperty.Register<EntityCollectionViewerOrganizerControl, ObservableCollectionEx<QueueAction>>( nameof( QueueActions ) );

        public EntityCollectionViewerOrganizerControl()
        {
            InitializeComponent();
        }

        public ItemCollection Collection
        {
            get => (ItemCollection) GetValue( CollectionProperty );
            set => SetValue( CollectionProperty, value );
        }

        public ObservableCollectionEx<QueueAction> QueueActions
        {
            get => (ObservableCollectionEx<QueueAction>) GetValue( QueueActionsProperty );
            set => SetValue( QueueActionsProperty, value );
        }

        private static void PropertyChangedCallback( AvaloniaObject d, AvaloniaPropertyChangedEventArgs e )
        {
            if ( !( d is EntityCollectionViewerOrganizerControl control ) )
            {
                return;
            }

            if ( !( control.DataContext is EntityCollectionViewerOrganizerViewModel viewModel ) )
            {
                return;
            }

            if ( e.Property == CollectionProperty )
            {
                viewModel.Collection = (ItemCollection) e.NewValue;
            }
            else if ( e.Property == QueueActionsProperty )
            {
                viewModel.QueueActions = (ObservableCollectionEx<QueueAction>) e.NewValue;
            }
        }
    }
}