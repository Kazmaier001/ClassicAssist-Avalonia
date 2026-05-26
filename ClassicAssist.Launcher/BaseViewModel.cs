using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;

namespace ClassicAssist.Launcher
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        protected Dispatcher _dispatcher;

        public BaseViewModel()
        {
            _dispatcher = Dispatcher.UIThread;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void SetProperty<T>( ref T obj, T value, [CallerMemberName] string propertyName = "" )
        {
            obj = value;
            NotifyPropertyChanged( propertyName );
        }

        protected void NotifyPropertyChanged( [CallerMemberName] string propertyName = "" )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }
    }

    // Avalonia has no WPF CommandManager; commands re-evaluate CanExecute on demand via
    // CanExecuteChanged. Call RaiseCanExecuteChanged() from VMs after state changes that affect it.
    public class RelayCommand : ICommand
    {
        private readonly Func<object, bool> _canExecute;
        private readonly Action<object> _execute;

        public RelayCommand( Action<object> execute, Func<object, bool> canExecute = null )
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute( object parameter ) => _canExecute == null || _canExecute( parameter );
        public void Execute( object parameter ) => _execute( parameter );

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke( this, EventArgs.Empty );
    }

    public class RelayCommandAsync : ICommand
    {
        private readonly Func<object, bool> _canExecute;
        private readonly Func<object, Task> _execute;

        public RelayCommandAsync( Func<object, Task> execute, Func<object, bool> canExecute )
        {
            _execute = execute ?? throw new ArgumentNullException( nameof( execute ) );
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute( object parameter ) => _canExecute == null || _canExecute( parameter );
        public async void Execute( object parameter ) => await _execute( parameter );

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke( this, EventArgs.Empty );
    }
}
