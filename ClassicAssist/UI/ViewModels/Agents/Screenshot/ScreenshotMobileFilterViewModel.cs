using System;
using ClassicAssist.Misc;
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

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;
using System.Windows.Input;
using Assistant;
using ClassicAssist.Data.Screenshot;
using ClassicAssist.Shared.UI;
using ClassicAssist.UO;
using ClassicAssist.UO.Data;
using ClassicAssist.UO.Objects;

namespace ClassicAssist.UI.ViewModels.Agents.Screenshot
{
    public class ScreenshotMobileFilterViewModel : BaseViewModel
    {
        private ICommand _addCommand;
        private bool _enabled;

        private ObservableCollection<ScreenshotMobileFilterEntry> _items =
            new ObservableCollection<ScreenshotMobileFilterEntry>();

        private ICommand _okCommand;

        private ICommand _removeCommand;

        private ScreenshotMobileFilterEntry _selectedItem;
        private ICommand _targetCommand;

        public ICommand AddCommand => _addCommand ?? ( _addCommand = new RelayCommand( Add ) );
        public bool? DialogResult { get; set; } = false;

        // Code-behind subscribes and calls Window.Close(). Avalonia's Click
        // fires BEFORE Command.Execute, so CloseOnClickBehaviour on the OK
        // button would shut the window down before Ok() can set DialogResult.
        // Closing from inside the command is the atomic fix — see the
        // avalonia-click-vs-command-order memory pin.
        public event Action RequestClose;

        public bool Enabled
        {
            get => _enabled;
            set => SetProperty( ref _enabled, value );
        }

        public ObservableCollection<ScreenshotMobileFilterEntry> Items
        {
            get => _items;
            set => SetProperty( ref _items, value );
        }

        public ICommand OkCommand => _okCommand ?? ( _okCommand = new RelayCommand( Ok, o => true ) );

        public ICommand RemoveCommand =>
            _removeCommand ?? ( _removeCommand = new RelayCommand( Remove, o => SelectedItem != null ) );

        public ScreenshotMobileFilterEntry SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty( ref _selectedItem, value );
                // Avalonia has no CommandManager.RequerySuggested, so a
                // RelayCommand predicate of `SelectedItem != null` never
                // re-evaluates on selection change. Nudge here.
                ( _removeCommand as RelayCommand )?.RaiseCanExecuteChanged();
            }
        }

        public ICommand TargetCommand =>
            _targetCommand ?? ( _targetCommand = new RelayCommandAsync( Target, o => Engine.Connected ) );

        private void Remove( object obj )
        {
            if ( SelectedItem != null )
            {
                Dispatcher.UIThread.Invoke( () => { Items.Remove( SelectedItem ); } );
            }
        }

        private void Add( object obj )
        {
            Dispatcher.UIThread.Invoke( () => { Items.Add( new ScreenshotMobileFilterEntry() ); } );
        }

        private void Ok( object obj )
        {
            DialogResult = true;
            RequestClose?.Invoke();
        }

        private async Task Target( object arg )
        {
            ( TargetType _, TargetFlags _, int serial, int _, int _, int _, int itemID ) =
                await Commands.GetTargetInfoAsync();

            if ( UOMath.IsMobile( serial ) )
            {
                Mobile mobile = Engine.Mobiles.GetMobile( serial );

                string name = mobile?.Name ?? "Unknown";

                Dispatcher.UIThread.Invoke( () =>
                {
                    Items.Add( new ScreenshotMobileFilterEntry { ID = itemID, Note = name } );
                } );
            }
        }
    }
}