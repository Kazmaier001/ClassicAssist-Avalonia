using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using ClassicAssist.Data.Hotkeys;
using static ClassicAssist.Misc.SDLKeys;

namespace ClassicAssist.UI.Controls
{
    /// <inheritdoc cref="UserControl" />
    /// <summary>
    ///     Focus-grabbing surface that captures any key combo (modifier + key,
    ///     including normally-edit-swallowed keys like Backspace or Ctrl+A) and
    ///     publishes it as a <see cref="ShortcutKeys" /> via <see cref="Shortcut" />.
    ///
    ///     Implemented over a focusable Border (not a TextBox) so the WPF
    ///     PreviewKeyDown semantics — where the textbox never gets a chance to
    ///     process edit shortcuts — are matched on Avalonia.
    /// </summary>
    public partial class TextBoxKey : UserControl
    {
        // BindingMode.TwoWay so {Binding SelectedItem.Hotkey} writes the
        // captured ShortcutKeys back to the source. Avalonia StyledProperty
        // defaults to OneWay; WPF's BindsTwoWayByDefault has no auto
        // equivalent ([[avalonia-two-way-binding]]). Without this the
        // Hotkeys tab Shortcut field captured keystrokes locally but never
        // pushed them onto HotkeyEntry.Hotkey, so the red→green circle
        // never flipped and nothing got saved.
        public static readonly StyledProperty<object> ShortcutProperty =
            AvaloniaProperty.Register<TextBoxKey, object>(
                nameof( Shortcut ),
                defaultBindingMode: BindingMode.TwoWay );

        // Modifier keys we track left/right separately. e.Key gives us the
        // physical side; KeyModifiers (the flag) doesn't.
        private static readonly Key[] ModifierKeys =
        {
            Key.LeftCtrl, Key.RightCtrl,
            Key.LeftShift, Key.RightShift,
            Key.LeftAlt, Key.RightAlt
        };

        private readonly HashSet<Key> _activeModifiers = new HashSet<Key>();

        public TextBoxKey()
        {
            InitializeComponent();

            // Clear modifier-key tracking when focus leaves the surface — otherwise a Ctrl
            // pressed while the textbox had focus and released elsewhere stays in
            // _activeModifiers and gets captured on the next mouse-wheel/button input.
            Border root = this.FindControl<Border>( "Root" );
            if ( root != null )
            {
                root.LostFocus += ( _, _ ) => _activeModifiers.Clear();
            }
        }

        public Key Key { get; private set; }
        public ModKey Modifier { get; set; }

        public object Shortcut
        {
            get => GetValue( ShortcutProperty );
            set => SetValue( ShortcutProperty, value );
        }

        private static bool IsModifier( Key key )
        {
            return ModifierKeys.Contains( key );
        }

        private ModKey CurrentModifier()
        {
            ModKey result = ModKey.None;
            foreach ( Key k in _activeModifiers )
            {
                switch ( k )
                {
                    case Key.LeftCtrl: result |= ModKey.LeftCtrl; break;
                    case Key.RightCtrl: result |= ModKey.RightCtrl; break;
                    case Key.LeftShift: result |= ModKey.LeftShift; break;
                    case Key.RightShift: result |= ModKey.RightShift; break;
                    case Key.LeftAlt: result |= ModKey.LeftAlt; break;
                    case Key.RightAlt: result |= ModKey.RightAlt; break;
                }
            }
            return result;
        }

        private void Root_KeyDown( object sender, KeyEventArgs e )
        {
            e.Handled = true;
            Key key = e.Key;

            if ( IsModifier( key ) )
            {
                _activeModifiers.Add( key );
                return;
            }

            Modifier = CurrentModifier();
            Key = key;
            Shortcut = new ShortcutKeys { Modifier = Modifier, Key = Key };
        }

        private void Root_KeyUp( object sender, KeyEventArgs e )
        {
            if ( IsModifier( e.Key ) )
            {
                _activeModifiers.Remove( e.Key );
                e.Handled = true;
            }
        }

        private void Root_PointerPressed( object sender, PointerPressedEventArgs e )
        {
            var props = e.GetCurrentPoint( null ).Properties;
            // Click anywhere on the surface: give it keyboard focus so subsequent
            // KeyDown events route here. Left/right clicks otherwise pass through
            // (the WPF version explicitly returned out of the mouse handler for
            // L/R buttons).
            ( sender as Border )?.Focus();

            if ( props.IsLeftButtonPressed || props.IsRightButtonPressed )
            {
                return;
            }

            e.Handled = true;

            Modifier = ResolveModifierFromEvent( e.KeyModifiers );
            Shortcut = new ShortcutKeys { Modifier = Modifier, Mouse = MouseOptions.MiddleButton };
        }

        private void Root_PointerWheelChanged( object sender, PointerWheelEventArgs e )
        {
            e.Handled = true;

            Modifier = ResolveModifierFromEvent( e.KeyModifiers );

            Shortcut = e.Delta.Y < 0
                ? new ShortcutKeys { Mouse = MouseOptions.MouseWheelDown, Modifier = Modifier }
                : new ShortcutKeys { Mouse = MouseOptions.MouseWheelUp, Modifier = Modifier };
        }

        // Trust e.KeyModifiers (OS-authoritative for whether Ctrl/Shift/Alt is currently held)
        // but consult _activeModifiers ONLY to resolve Left vs Right side. If _activeModifiers
        // has a stale LeftCtrl entry (e.g. user pressed Ctrl, focus moved, Ctrl was released
        // elsewhere so KeyUp never fired here) but KeyModifiers says no Control, we ignore the
        // stale bit — which is what was producing spurious "Ctrl + Wheel" captures.
        private ModKey ResolveModifierFromEvent( KeyModifiers km )
        {
            ModKey result = ModKey.None;
            if ( km.HasFlag( KeyModifiers.Control ) )
                result |= _activeModifiers.Contains( Key.RightCtrl ) ? ModKey.RightCtrl : ModKey.LeftCtrl;
            if ( km.HasFlag( KeyModifiers.Shift ) )
                result |= _activeModifiers.Contains( Key.RightShift ) ? ModKey.RightShift : ModKey.LeftShift;
            if ( km.HasFlag( KeyModifiers.Alt ) )
                result |= _activeModifiers.Contains( Key.RightAlt ) ? ModKey.RightAlt : ModKey.LeftAlt;
            return result;
        }
    }
}
