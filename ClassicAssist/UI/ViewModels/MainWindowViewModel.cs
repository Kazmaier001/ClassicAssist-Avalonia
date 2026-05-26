using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Windows.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Assistant;
using ClassicAssist.Data;
using ClassicAssist.Shared.Resources;
using ClassicAssist.Shared.UI;
using ClassicAssist.UI.Views;

namespace ClassicAssist.UI.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private ICommand _debugCommand;
        private DebugWindow _debugWindow;
        private ICommand _minimizeCommand;
        private ICommand _restoreWindowCommand;
        private string _status = Strings.Ready___;
        private string _title = Strings.ProductName;
        private Window _minimizedWindow;
        private TrayIcon _trayIcon;

        public MainWindowViewModel()
        {
            Engine.Dispatcher = Dispatcher.UIThread;
            Engine.UpdateWindowTitleEvent += OnUpdateWindowTitleEvent;
            Engine.ClientClosing += OnClientClosing;
        }

        public ICommand DebugCommand =>
            _debugCommand ?? ( _debugCommand = new RelayCommand( ShowDebugWindow, o => true ) );

        public ICommand MinimizeCommand =>
            _minimizeCommand ?? ( _minimizeCommand = new RelayCommand( Minimize, o => true ) );

        public ICommand RestoreWindowCommand =>
            _restoreWindowCommand ?? ( _restoreWindowCommand = new RelayCommand( RestoreWindow, o => true ) );

        public string Status
        {
            get => _status;
            set => SetProperty( ref _status, value );
        }

        public string Title
        {
            get => _title;
            set => SetProperty( ref _title, value );
        }

        private void OnClientClosing()
        {
            if ( _trayIcon != null )
            {
                Dispatcher.UIThread.Post( () => _trayIcon.IsVisible = false );
            }
        }

        private void OnUpdateWindowTitleEvent()
        {
            Title = string.IsNullOrEmpty( Engine.Player?.Name )
                ? Strings.ProductName
                : $"{Engine.Player?.Name} - {( Options.CurrentOptions.ShowProfileNameWindowTitle ? $"({Options.CurrentOptions.Name}) - " : "" )}{Strings.ProductName}";

            if ( _trayIcon != null )
            {
                Dispatcher.UIThread.Post( () => _trayIcon.ToolTipText = Title );
            }
        }

        private void ShowDebugWindow( object obj )
        {
            _debugWindow = new DebugWindow();
            _debugWindow.Show();
        }

        private void Minimize( object obj )
        {
            if ( !( obj is Window window ) )
            {
                return;
            }

            _minimizedWindow = window;

            if ( !Options.CurrentOptions.SysTray )
            {
                window.WindowState = WindowState.Minimized;
                return;
            }

            EnsureTrayIcon( window );

            _trayIcon.ToolTipText = Title;
            _trayIcon.IsVisible = true;
            window.ShowInTaskbar = false;
            window.Hide();
        }

        private void RestoreWindow( object obj )
        {
            if ( !( obj is Window window ) )
            {
                window = _minimizedWindow;
            }

            if ( window == null )
            {
                return;
            }

            window.ShowInTaskbar = true;
            window.WindowState = WindowState.Normal;
            window.Show();
            window.Activate();

            if ( _trayIcon != null )
            {
                _trayIcon.IsVisible = false;
            }
        }

        private void EnsureTrayIcon( Window window )
        {
            if ( _trayIcon != null )
            {
                return;
            }

            WindowIcon icon = null;

            try
            {
                using ( var stream = AssetLoader.Open( new Uri( "avares://ClassicAssist/Resources/logo.ico" ) ) )
                {
                    icon = new WindowIcon( stream );
                }
            }
            catch ( Exception )
            {
                // logo asset missing — tray icon will use platform default
            }

            _trayIcon = new TrayIcon
            {
                Icon = icon,
                ToolTipText = Title,
                IsVisible = false
            };

            _trayIcon.Clicked += ( _, _ ) => RestoreWindow( window );

            var icons = TrayIcon.GetIcons( Application.Current ) ?? new TrayIcons();
            icons.Add( _trayIcon );
            TrayIcon.SetIcons( Application.Current, icons );
        }
    }
}