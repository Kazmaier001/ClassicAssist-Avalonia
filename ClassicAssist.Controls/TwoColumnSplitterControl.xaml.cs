// Copyright (C) 2023 Reetus
//  
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

using Avalonia;
using Avalonia.Controls;

namespace ClassicAssist.Controls
{
    /// <summary>
    ///     Interaction logic for TwoColumnSplitterControl.axaml
    /// </summary>
    public partial class TwoColumnSplitterControl : UserControl
    {
        public static readonly StyledProperty<object> LeftContentProperty =
            AvaloniaProperty.Register<TwoColumnSplitterControl, object>( nameof( LeftContent ) );

        public static readonly StyledProperty<object> RightContentProperty =
            AvaloniaProperty.Register<TwoColumnSplitterControl, object>( nameof( RightContent ) );

        public static readonly StyledProperty<GridLength> LeftContentWidthProperty =
            AvaloniaProperty.Register<TwoColumnSplitterControl, GridLength>( nameof( LeftContentWidth ),
                new GridLength( 1, GridUnitType.Star ) );

        public TwoColumnSplitterControl()
        {
            InitializeComponent();
        }

        public object LeftContent
        {
            get => GetValue( LeftContentProperty );
            set => SetValue( LeftContentProperty, value );
        }

        public GridLength LeftContentWidth
        {
            get => GetValue( LeftContentWidthProperty );
            set => SetValue( LeftContentWidthProperty, value );
        }

        public object RightContent
        {
            get => GetValue( RightContentProperty );
            set => SetValue( RightContentProperty, value );
        }
    }
}