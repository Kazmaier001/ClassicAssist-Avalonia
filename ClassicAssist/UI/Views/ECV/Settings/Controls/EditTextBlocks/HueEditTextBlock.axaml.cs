// Copyright (C) 2024 Reetus
//  
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;

namespace ClassicAssist.UI.Views.ECV.Settings.Controls.EditTextBlocks
{
    /// <summary>
    ///     Interaction logic for HueEditTextBlock.xaml
    /// </summary>
    public partial class HueEditTextBlock : UserControl, INotifyPropertyChanged
    {
        public static readonly StyledProperty<int> HueProperty = AvaloniaProperty.Register<HueEditTextBlock, int>( nameof( Hue ) );

        private string _label;

        public HueEditTextBlock()
        {
            InitializeComponent();

            UpdateLabel( this, Hue );
        }

        public int Hue
        {
            get => (int) GetValue( HueProperty );
            set => SetValue( HueProperty, value );
        }

        public string Label
        {
            get => _label;
            set => SetField( ref _label, value );
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private static void UpdateLabel( HueEditTextBlock block, int id )
        {
            block.Label = id == -1 ? "Any" : $"{id}";
        }

        private static void HueChangedCallback( AvaloniaObject d, AvaloniaPropertyChangedEventArgs e )
        {
            if ( !( e.NewValue is int id ) )
            {
                return;
            }

            if ( e.OldValue is int oldValue && oldValue == id )
            {
                return;
            }

            if ( !( d is HueEditTextBlock block ) )
            {
                return;
            }

            block.Hue = id;
            UpdateLabel( block, id );
        }

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