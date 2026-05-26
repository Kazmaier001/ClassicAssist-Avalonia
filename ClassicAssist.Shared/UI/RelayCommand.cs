#region License

// Copyright (C) 2020 Reetus
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
using System.Windows.Input;

namespace ClassicAssist.Shared.UI
{
    public class RelayCommand : ICommand
    {
        private readonly Func<object, bool> _canExecute;
        private readonly Action<object> _execute;

        public RelayCommand( Action<object> execute, Func<object, bool> canExecute = null )
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute( object parameter )
        {
            return _canExecute == null || _canExecute( parameter );
        }

        public void Execute( object parameter )
        {
            _execute( parameter );
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke( this, EventArgs.Empty );
        }
    }

    public class RelayCommandAsync : ICommand
    {
        private readonly Func<object, bool> _canExecute;
        private readonly Func<object, System.Threading.Tasks.Task> _execute;

        public RelayCommandAsync( Func<object, System.Threading.Tasks.Task> execute, Func<object, bool> canExecute = null )
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute( object parameter )
        {
            return _canExecute == null || _canExecute( parameter );
        }

        public async void Execute( object parameter )
        {
            await _execute( parameter );
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke( this, EventArgs.Empty );
        }
    }
}