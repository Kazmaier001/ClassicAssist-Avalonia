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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

// https://stackoverflow.com/questions/4305565/wpf-context-menu-on-left-click
namespace ClassicAssist.UI.Misc.Behaviours
{
    public class ClickOpensContextMenuBehaviour
    {
        public static readonly AttachedProperty<bool> EnabledProperty =
            AvaloniaProperty.RegisterAttached<ClickOpensContextMenuBehaviour, Control, bool>( "Enabled" );

        static ClickOpensContextMenuBehaviour()
        {
            // Avalonia attached-property change callbacks aren't auto-wired the way WPF's
            // PropertyMetadata(callback) is — without this static-ctor subscription, the
            // XAML setter is inert and the Image/Button never gets its click handler.
            EnabledProperty.Changed.AddClassHandler<Control>( HandlePropertyChanged );
        }

        public static bool GetEnabled( AvaloniaObject obj ) => (bool) obj.GetValue( EnabledProperty );

        public static void SetEnabled( AvaloniaObject obj, bool value ) => obj.SetValue( EnabledProperty, value );

        private static void HandlePropertyChanged( Control control, AvaloniaPropertyChangedEventArgs args )
        {
            bool enabled = args.GetNewValue<bool>();

            switch ( control )
            {
                case Image image:
                    image.PointerPressed -= OnImagePressed;
                    if ( enabled )
                    {
                        image.PointerPressed += OnImagePressed;
                    }
                    break;
                case Button button:
                    // Use Click (not PointerPressed) so the existing context menu doesn't
                    // swallow our open-call as a re-press of itself.
                    button.Click -= OnButtonClicked;
                    if ( enabled )
                    {
                        button.Click += OnButtonClicked;
                    }
                    break;
            }
        }

        private static void OnImagePressed( object sender, PointerPressedEventArgs args )
        {
            if ( sender is Image { ContextMenu: { } menu } image && GetEnabled( image ) )
            {
                menu.Open( image );
            }
        }

        private static void OnButtonClicked( object sender, RoutedEventArgs args )
        {
            if ( sender is Button { ContextMenu: { } menu } button && GetEnabled( button ) )
            {
                menu.Open( button );
            }
        }
    }
}
