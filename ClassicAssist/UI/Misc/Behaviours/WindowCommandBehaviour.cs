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

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using System.Windows.Input;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.UI.Misc.Behaviours
{
    public enum WindowCommand
    {
        Minimize,
        Maximize,
        Close
    }

    public class WindowCommandBehaviour : Behavior<Button>
    {
        public static readonly StyledProperty<WindowCommand> CommandProperty = AvaloniaProperty.Register<WindowCommandBehaviour, WindowCommand>( nameof( Command ) );

        public static readonly StyledProperty<ICommand> MinimizeCommandProperty = AvaloniaProperty.Register<WindowCommandBehaviour, ICommand>( nameof( MinimizeCommand ) );

        public WindowCommand Command
        {
            get => (WindowCommand) GetValue( CommandProperty );
            set => SetValue( CommandProperty, value );
        }

        public ICommand MinimizeCommand
        {
            get => (ICommand) GetValue( MinimizeCommandProperty );
            set => SetValue( MinimizeCommandProperty, value );
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Click += OnClick;
        }

        private void OnClick( object sender, RoutedEventArgs e )
        {
            if ( !( sender is Control element ) )
            {
                return;
            }

            Window window = TopLevel.GetTopLevel( element ) as Window;

            if ( window == null )
            {
                return;
            }

            switch ( Command )
            {
                case WindowCommand.Minimize:
                {
                    if ( MinimizeCommand != null )
                    {
                        MinimizeCommand.Execute( window );
                        break;
                    }

                    window.WindowState = WindowState.Minimized;
                    break;
                }
                case WindowCommand.Maximize:
                {
                    window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

                    break;
                }
                case WindowCommand.Close:
                {
                    window.Close();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Click -= OnClick;
        }
    }
}