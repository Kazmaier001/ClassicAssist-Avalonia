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

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace ClassicAssist.Controls.Headered
{
    public class HorizontalHeaderedComboBox : HorizontalHeaderedContentControl
    {
        public static readonly StyledProperty<object> SelectedItemProperty =
            AvaloniaProperty.Register<HorizontalHeaderedComboBox, object>( nameof( SelectedItem ),
                defaultBindingMode: BindingMode.TwoWay );

        public static readonly StyledProperty<IEnumerable> ItemsSourceProperty =
            AvaloniaProperty.Register<HorizontalHeaderedComboBox, IEnumerable>( nameof( ItemsSource ) );

        public HorizontalHeaderedComboBox()
        {
            // [!X] = this[!Y] creates a OneWay binding (Y -> X). For SelectedItem we
            // need TwoWay so user picks in the inner ComboBox flow back through this
            // control's SelectedItem to the bound source property. Use [!!] (TwoWay
            // shorthand) or the binding silently swallows user picks — matches the
            // WPF source which explicitly sets Mode=BindingMode.TwoWay.
            ComboBox comboBox = new ComboBox
            {
                [!ItemsControl.ItemsSourceProperty] = this[!ItemsSourceProperty],
                [!!SelectingItemsControl.SelectedItemProperty] = this[!!SelectedItemProperty]
            };
            Content = comboBox;
        }

        public IEnumerable ItemsSource
        {
            get => GetValue( ItemsSourceProperty );
            set => SetValue( ItemsSourceProperty, value );
        }

        public object SelectedItem
        {
            get => GetValue( SelectedItemProperty );
            set => SetValue( SelectedItemProperty, value );
        }
    }
}