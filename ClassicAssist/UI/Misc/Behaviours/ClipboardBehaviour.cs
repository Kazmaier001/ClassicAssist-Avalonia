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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Windows.Input;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.UI.Misc.Behaviours
{
    public class ClipboardBehaviour : Behavior<Control>
    {
        public static readonly StyledProperty<ICommand> CopyCommandProperty =
            AvaloniaProperty.Register<ClipboardBehaviour, ICommand>( nameof( CopyCommand ) );

        public static readonly StyledProperty<ICommand> PasteCommandProperty =
            AvaloniaProperty.Register<ClipboardBehaviour, ICommand>( nameof( PasteCommand ) );

        public static readonly StyledProperty<object> CommandParameterProperty =
            AvaloniaProperty.Register<ClipboardBehaviour, object>( nameof( CommandParameter ) );

        public object CommandParameter
        {
            get => GetValue( CommandParameterProperty );
            set => SetValue( CommandParameterProperty, value );
        }

        public ICommand CopyCommand
        {
            get => GetValue( CopyCommandProperty );
            set => SetValue( CopyCommandProperty, value );
        }

        public ICommand PasteCommand
        {
            get => GetValue( PasteCommandProperty );
            set => SetValue( PasteCommandProperty, value );
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.KeyDown += OnKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.KeyDown -= OnKeyDown;
        }

        private void OnKeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyModifiers.HasFlag( KeyModifiers.Control ) )
            {
                switch ( e.Key )
                {
                    case Key.C:
                        CopyCommand?.Execute( CommandParameter );
                        e.Handled = true;
                        break;
                    case Key.V:
                        PasteCommand?.Execute( CommandParameter );
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}