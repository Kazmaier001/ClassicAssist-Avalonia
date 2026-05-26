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
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Windows.Input;
using Assistant;
using ClassicAssist.Data.Hotkeys;
using ClassicAssist.Data.Hotkeys.Commands;
using ClassicAssist.Misc;
using ClassicAssist.Shared.UI;
using ClassicAssist.UI.Views.Debug;
using ClassicAssist.UO;
using Newtonsoft.Json;
using KeyEventArgs = Avalonia.Input.KeyEventArgs;

namespace ClassicAssist.UI.ViewModels.Debug
{
    public class DebugKeyboardViewModel : BaseViewModel
    {
        private readonly DebugKeyboardControl _control;
        private ObservableCollection<FailKey> _failKeys = new ObservableCollection<FailKey>();
        private int _keyboardLayoutId;
        private string _keyboardName;
        private ICommand _removeItemCommand;
        private ICommand _saveCommand;
        private int _sdlKey;
        private int _sdlMod;
        private FailKey _selectedItem;
        private string _status;
        private ICommand _testKeyCommand;
        private Key _uoKey;
        private Key _wpfKey;

        public DebugKeyboardViewModel()
        {
            // InputLanguageManager removed (not available in Avalonia)
            // InputLanguageManager removed (not available in Avalonia)
        }

        public DebugKeyboardViewModel( DebugKeyboardControl control )
        {
            // InputLanguageManager removed (not available in Avalonia)
            // InputLanguageManager removed (not available in Avalonia)
            _control = control;

            // SaveCommand.CanExecute reads FailKeys.Count > 0; without this
            // listener it stays grayed forever because the predicate is only
            // evaluated at command construction (when the collection is empty).
            _failKeys.CollectionChanged += ( s, e ) =>
                ( _saveCommand as RelayCommandAsync )?.RaiseCanExecuteChanged();
        }

        public ObservableCollection<FailKey> FailKeys
        {
            get => _failKeys;
            set => SetProperty( ref _failKeys, value );
        }

        public int KeyboardLayoutId
        {
            get => _keyboardLayoutId;
            set => SetProperty( ref _keyboardLayoutId, value );
        }

        public string KeyboardName
        {
            get => _keyboardName;
            set => SetProperty( ref _keyboardName, value );
        }

        public ICommand RemoveItemCommand =>
            _removeItemCommand ?? ( _removeItemCommand = new RelayCommand( RemoveItem, o => true ) );

        public ICommand SaveCommand =>
            _saveCommand ?? ( _saveCommand = new RelayCommandAsync( Save, o => FailKeys.Count > 0 ) );

        public int SDLKey
        {
            get => _sdlKey;
            set => SetProperty( ref _sdlKey, value );
        }

        public int SDLMod
        {
            get => _sdlMod;
            set => SetProperty( ref _sdlMod, value );
        }

        public FailKey SelectedItem
        {
            get => _selectedItem;
            set => SetProperty( ref _selectedItem, value );
        }

        public string Status
        {
            get => _status;
            set => SetProperty( ref _status, value );
        }

        public ICommand TestKeyCommand =>
            _testKeyCommand ?? ( _testKeyCommand = new RelayCommandAsync( TestKey, o => Engine.Connected ) );

        public Key UOKey
        {
            get => _uoKey;
            set => SetProperty( ref _uoKey, value );
        }

        public Key WPFKey
        {
            get => _wpfKey;
            set => SetProperty( ref _wpfKey, value );
        }

        private void RemoveItem( object obj )
        {
            if ( !( obj is FailKey failKey ) )
            {
                return;
            }

            FailKeys.Remove( failKey );
        }

        private async Task TestKey( object arg )
        {
            Status = string.Empty;
            // Auto-focusing CUO is Windows-only. On Linux/macOS the user just has to
            // click into the CUO window manually; the in-game system message below
            // still tells them what to do.
            if ( OperatingSystem.IsWindows() )
            {
                NativeMethods.SetForegroundWindow( Engine.WindowHandle );
            }
            Commands.SystemMessage( "Press key to test..." );

            AutoResetEvent are = new AutoResetEvent( false );

            void OnHotkeyPressed( int key, int mod, Key keys, SDLKeys.ModKey modKey )
            {
                Engine.HotkeyPressedEvent -= OnHotkeyPressed;

                UOKey = keys;
                SDLKey = key;
                SDLMod = mod;
                are.Set();
            }

            HotkeyManager manager = HotkeyManager.GetInstance();

            if ( manager.Enabled )
            {
                new ToggleHotkeys().Execute();
            }

            Engine.HotkeyPressedEvent += OnHotkeyPressed;
            bool result = false;

            await Task.Run( () => { result = are.WaitOne( 15000 ); } );

            if ( !manager.Enabled )
            {
                new ToggleHotkeys().Execute();
            }

            if ( !result )
            {
                Status = "No UO keypress detected.";
                return;
            }

            void OnWpfKeyDown( KeyEventArgs args )
            {
                _control.WPFKeyDownEvent -= OnWpfKeyDown;

                Key key = args.Key;

                switch ( key )
                {
                    case Key.DeadCharProcessed:
                        key = args.Key;
                        break;
                    case Key.ImeProcessed:
                        key = args.Key;
                        break;
                    case Key.System:
                        key = args.Key;
                        break;
                }

                WPFKey = key;
                are.Set();
            }

            Window window = TopLevel.GetTopLevel( _control ) as Window;
            window?.Activate();
            _control.Focus();

            if ( _control != null )
            {
                Status = "* Press the same key again *";
                _control.WPFKeyDownEvent += OnWpfKeyDown;
            }

            await Task.Run( () => { result = are.WaitOne( 15000 ); } );

            if ( !result )
            {
                Status = "No WPF keypress detected.";
                return;
            }

            if ( UOKey != WPFKey )
            {
                Status = "Key didn't match";
                FailKeys.Add( new FailKey
                {
            // InputLanguageManager removed (not available in Avalonia)
            // InputLanguageManager removed (not available in Avalonia)
                    WPFKey = WPFKey,
                    UOKey = UOKey,
                    SDLKey = SDLKey,
                    SDLMod = SDLMod
                } );
            }
            else
            {
                Status = "Key matched";
            }
        }

        private async Task Save( object arg )
        {
            ClassicAssist.Misc.SaveFileDialog fsd = new ClassicAssist.Misc.SaveFileDialog { OverwritePrompt = true, FileName = "keys.json" };

            bool? result = fsd.ShowDialog();

            if ( result == true )
            {
                string json = JsonConvert.SerializeObject( FailKeys );

                File.WriteAllText( fsd.FileName, json );
            }

            await Task.CompletedTask;
        }
    }

    public class FailKey : SetPropertyNotifyChanged
    {
        private int _keyboardLayoutId;
        private string _keyboardName;
        private int _sdlKey;
        private int _sdlMod;
        private Key _uoKey;
        private Key _wpfKey;

        public int KeyboardLayoutId
        {
            get => _keyboardLayoutId;
            set => SetProperty( ref _keyboardLayoutId, value );
        }

        public string KeyboardName
        {
            get => _keyboardName;
            set => SetProperty( ref _keyboardName, value );
        }

        public int SDLKey
        {
            get => _sdlKey;
            set => SetProperty( ref _sdlKey, value );
        }

        public int SDLMod
        {
            get => _sdlMod;
            set => SetProperty( ref _sdlMod, value );
        }

        public Key UOKey
        {
            get => _uoKey;
            set => SetProperty( ref _uoKey, value );
        }

        public string UOKeyString => UOKey.ToString();

        public Key WPFKey
        {
            get => _wpfKey;
            set => SetProperty( ref _wpfKey, value );
        }

        public string WPFKeyString => WPFKey.ToString();
    }
}