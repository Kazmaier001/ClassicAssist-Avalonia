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
using Avalonia.Data;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.UI.Misc.Behaviours
{
    public class AvalonBindingBehaviour : Behavior<TextEditor>
    {
        // TwoWay so writes from this behaviour (OnLoaded → DocumentBinding =
        // textEditor.Document; CaretOnPositionChanged → CaretBinding = offset)
        // actually propagate back to the bound source on the userControl.
        public static readonly StyledProperty<TextDocument> DocumentBindingProperty = AvaloniaProperty.Register<AvalonBindingBehaviour, TextDocument>( nameof( DocumentBinding ), defaultBindingMode: BindingMode.TwoWay );

        public static readonly StyledProperty<int> CaretBindingProperty = AvaloniaProperty.Register<AvalonBindingBehaviour, int>( nameof( CaretBinding ), defaultBindingMode: BindingMode.TwoWay );

        public int CaretBinding
        {
            get => (int) GetValue( CaretBindingProperty );
            set => SetValue( CaretBindingProperty, value );
        }

        public TextDocument DocumentBinding
        {
            get => (TextDocument) GetValue( DocumentBindingProperty );
            set => SetValue( DocumentBindingProperty, value );
        }

        private static void PropertyChangedCallback( AvaloniaObject d, AvaloniaPropertyChangedEventArgs e )
        {
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.TextArea.Caret.PositionChanged += CaretOnPositionChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.TextArea.Caret.PositionChanged -= CaretOnPositionChanged;
        }

        private void CaretOnPositionChanged( object sender, EventArgs e )
        {
            if ( sender is Caret caret )
            {
                CaretBinding = caret.Offset;
            }
        }

        private void OnLoaded( object sender, RoutedEventArgs args )
        {
            if ( sender is TextEditor textEditor )
            {
                DocumentBinding = textEditor.Document;
            }
        }
    }
}