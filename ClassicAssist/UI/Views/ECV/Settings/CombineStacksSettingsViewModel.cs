#region License

// Copyright (C) 2024 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

#endregion

using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Input;
using System.Windows.Input;
using Avalonia.Threading;
using ClassicAssist.Shared.UI;
using ClassicAssist.UI.Views.ECV.Settings.Models;
using ClassicAssist.UO.Data;

namespace ClassicAssist.UI.Views.ECV.Settings
{
    public class CombineStacksSettingsViewModel : SetPropertyNotifyChanged
    {
        private readonly Dispatcher _dispatcher;
        private ICommand _addEntryCommand;
        private ObservableCollection<CombineStacksOpenContainersIgnoreEntry> _items = new ObservableCollection<CombineStacksOpenContainersIgnoreEntry>();
        private ICommand _removeEntryCommand;
        private CombineStacksOpenContainersIgnoreEntry _selectedItem;

        public CombineStacksSettingsViewModel()
        {
            _dispatcher = Dispatcher.UIThread;

            if ( Avalonia.Controls.Design.IsDesignMode )
            {
                Cliloc.Initialize( @"C:\Users\johns\Documents\UO\Ultima Online Classic" );
            }
        }

        public ICommand AddEntryCommand => _addEntryCommand ?? ( _addEntryCommand = new RelayCommand( AddEntry ) );

        public ObservableCollection<CombineStacksOpenContainersIgnoreEntry> Items
        {
            get => _items;
            set => SetProperty( ref _items, value );
        }

        public ICommand RemoveEntryCommand => _removeEntryCommand ?? ( _removeEntryCommand = new RelayCommand( RemoveEntry, o => o != null ) );

        public CombineStacksOpenContainersIgnoreEntry SelectedItem
        {
            get => _selectedItem;
            set => SetProperty( ref _selectedItem, value );
        }

        private void RemoveEntry( object obj )
        {
            if ( !( obj is CombineStacksOpenContainersIgnoreEntry entry ) )
            {
                return;
            }

            Dispatcher.UIThread.Invoke( () => { Items.Remove( entry ); } );
        }

        private void AddEntry( object obj )
        {
            Dispatcher.UIThread.Invoke( () => Items.Add( new CombineStacksOpenContainersIgnoreEntry() ) );
        }
    }
}