#region License

// Copyright (C) 2023 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

#endregion

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace ClassicAssist.Controls.Headered
{
    public class HorizontalHeaderedTextBox : HorizontalHeaderedContentControl
    {
        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<HorizontalHeaderedTextBox, string>( nameof( Text ),
                defaultBindingMode: BindingMode.TwoWay );

        public HorizontalHeaderedTextBox()
        {
            // [!X] = this[!Y] creates a OneWay binding (Y -> X). For Text we need
            // TwoWay so user keystrokes in the inner TextBox flow back through this
            // control's Text to the bound source property. Use [!!] (TwoWay
            // shorthand) — same root cause as HorizontalHeaderedComboBox.SelectedItem.
            TextBox textBox = new TextBox
            {
                [!!TextBox.TextProperty] = this[!!TextProperty]
            };
            Content = textBox;
        }

        public string Text
        {
            get => GetValue( TextProperty );
            set => SetValue( TextProperty, value );
        }
    }
}