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

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;

namespace ClassicAssist.Controls
{
    public class ImageButton : Button
    {
        protected override Type StyleKeyOverride => typeof( Button );

        public static readonly StyledProperty<IImage> ImageSourceProperty =
            AvaloniaProperty.Register<ImageButton, IImage>( nameof( ImageSource ) );

        public static readonly StyledProperty<double> ImageHeightProperty =
            AvaloniaProperty.Register<ImageButton, double>( nameof( ImageHeight ), 16.0 );

        public ImageButton()
        {
            var image = new Image
            {
                Stretch = Stretch.Uniform,
                [!Image.SourceProperty] = this[!ImageSourceProperty],
                [!HeightProperty] = this[!ImageHeightProperty]
            };

            Content = image;
            Classes.Add( "imagebutton" );
        }

        public double ImageHeight
        {
            get => GetValue( ImageHeightProperty );
            set => SetValue( ImageHeightProperty, value );
        }

        public IImage ImageSource
        {
            get => GetValue( ImageSourceProperty );
            set => SetValue( ImageSourceProperty, value );
        }

        // Bulletproof transparency: the global `Button /template/ ContentPresenter`
        // style in DarkTheme.axaml sets Background=#333333 on every Button's inner
        // ContentPresenter. Class-selector overrides (`Button.imagebutton …`) are
        // supposed to win by specificity, but in practice the fill kept showing
        // through on the Cliloc/ItemID +/X buttons. Walk the template after it
        // applies and force the ContentPresenter (and inner Border, if any) to
        // transparent as local values, which beat all style values.
        protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
        {
            base.OnApplyTemplate( e );

            ContentPresenter cp = e.NameScope.Find<ContentPresenter>( "PART_ContentPresenter" );
            if ( cp != null )
            {
                cp.Background = Brushes.Transparent;
                cp.BorderBrush = Brushes.Transparent;
                cp.BorderThickness = new Thickness( 0 );
            }
        }
    }
}