using Avalonia.Controls;
// Copyright (C) $CURRENT_YEAR$ Reetus
//  
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ClassicAssist.Misc;

namespace ClassicAssist.UI.Views
{
    /// <summary>
    ///     Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window, INotifyPropertyChanged
    {
        public IImage Image { get; set; }

        public SplashWindow()
        {
            InitializeComponent();

            // Splash logo from AvaloniaResource. Assign directly to the named Image element
            // rather than via FindAncestor binding — the Image property has no INPC and the
            // binding raced past the ctor assignment in practice.
            Image = new Bitmap( AssetLoader.Open( new Uri( "avares://ClassicAssist/Resources/splash_logo.png" ) ) );
            SplashImage.Source = Image;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged( [CallerMemberName] string propertyName = null )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

        protected bool SetField<T>( ref T field, T value, [CallerMemberName] string propertyName = null )
        {
            if ( EqualityComparer<T>.Default.Equals( field, value ) )
            {
                return false;
            }

            field = value;
            OnPropertyChanged( propertyName );
            return true;
        }
    }
}