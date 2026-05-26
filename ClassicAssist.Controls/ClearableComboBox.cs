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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ClassicAssist.Controls
{
    // Subclasses ComboBox to add an overlay "X" clear button on the right side of the
    // selection box (left of the dropdown arrow) — matches the WPF ClearableComboBox.
    //
    // We use a plain Border+Image (NOT Avalonia.Button) so the Fluent Button hover/press
    // fill doesn't paint over our overlay. The Border's own background stays Transparent,
    // letting the ComboBox's hover color show through.
    public class ClearableComboBox : ComboBox
    {
        protected override Type StyleKeyOverride => typeof( ComboBox );

        public static readonly StyledProperty<ICommand> ClearCommandProperty =
            AvaloniaProperty.Register<ClearableComboBox, ICommand>( nameof( ClearCommand ) );

        public static readonly StyledProperty<object> ClearCommandParameterProperty =
            AvaloniaProperty.Register<ClearableComboBox, object>( nameof( ClearCommandParameter ) );

        private Border _clearButton;

        public ICommand ClearCommand
        {
            get => GetValue( ClearCommandProperty );
            set => SetValue( ClearCommandProperty, value );
        }

        public object ClearCommandParameter
        {
            get => GetValue( ClearCommandParameterProperty );
            set => SetValue( ClearCommandParameterProperty, value );
        }

        protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
        {
            base.OnApplyTemplate( e );

            // Defer until the template visuals are realized so GetVisualChildren is populated.
            Dispatcher.UIThread.Post( InjectClearButton, DispatcherPriority.Loaded );
        }

        private void InjectClearButton()
        {
            if ( _clearButton != null && _clearButton.GetVisualParent() != null )
                return;

            // Fluent ComboBox template: ContentPresenter#PART_ContentPresenter contains a
            // Grid whose children include Border#Background and PathIcon#DropDownGlyph.
            // Pick that Grid — NOT the outer DockPanel from DataValidationErrors, NOT any
            // Grid nested inside the dropdown Popup.
            Panel host = null;
            foreach ( var v in this.GetVisualDescendants() )
            {
                if ( v is not Grid g ) continue;
                if ( v.FindAncestorOfType<Popup>() != null ) continue;
                bool hasGlyph = false;
                foreach ( var c in g.Children )
                {
                    if ( c is PathIcon pi && pi.Name == "DropDownGlyph" )
                    { hasGlyph = true; break; }
                }
                if ( hasGlyph ) { host = g; break; }
            }
            if ( host == null )
                return;

            IImage icon = null;
            if ( this.TryFindResource( "ClearIcon", out var res ) && res is IImage img )
                icon = img;
            else if ( Application.Current != null &&
                      Application.Current.TryFindResource( "ClearIcon", out var res2 ) && res2 is IImage img2 )
                icon = img2;

            _clearButton = new Border
            {
                Name = "ClearButton",
                Background = Brushes.Transparent,
                Width = 16,
                Height = 16,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                // Leave room for the dropdown arrow (~28 DIP on Fluent).
                Margin = new Thickness( 0, 0, 28, 0 ),
                Cursor = new Cursor( StandardCursorType.Hand ),
                Child = new Image
                {
                    Source = icon,
                    Stretch = Stretch.Uniform,
                    Width = 10,
                    Height = 10
                }
            };

            // Span all template-Grid columns so HA=Right is relative to the whole control.
            if ( host is Grid hg && hg.ColumnDefinitions.Count > 0 )
                Grid.SetColumnSpan( _clearButton, hg.ColumnDefinitions.Count );

            // Capture the release so the ComboBox doesn't also toggle its dropdown.
            _clearButton.PointerReleased += OnClearPointerReleased;

            host.Children.Add( _clearButton );
        }

        private void OnClearPointerReleased( object sender, PointerReleasedEventArgs e )
        {
            if ( e.InitialPressMouseButton != MouseButton.Left )
                return;
            SelectedItem = null;
            ClearCommand?.Execute( ClearCommandParameter );
            e.Handled = true;
        }
    }
}
