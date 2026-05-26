// Copyright (C) 2024 Reetus
//  
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using ClassicAssist.UI.Views.ECV.Settings.Models;

namespace ClassicAssist.UI.Views.ECV.Settings
{
    /// <summary>
    ///     Interaction logic for CombineStacksSettingsControl.xaml
    /// </summary>
    public partial class CombineStacksSettingsControl : UserControl
    {
        public static readonly StyledProperty<ObservableCollection<CombineStacksOpenContainersIgnoreEntry>> ItemsProperty =
            AvaloniaProperty.Register<CombineStacksSettingsControl, ObservableCollection<CombineStacksOpenContainersIgnoreEntry>>( nameof( Items ) );

        static CombineStacksSettingsControl()
        {
            ItemsProperty.Changed.AddClassHandler<CombineStacksSettingsControl>( PropertyChangedCallback );
        }

        public CombineStacksSettingsControl()
        {
            InitializeComponent();
        }

        public ObservableCollection<CombineStacksOpenContainersIgnoreEntry> Items
        {
            get => (ObservableCollection<CombineStacksOpenContainersIgnoreEntry>) GetValue( ItemsProperty );
            set => SetValue( ItemsProperty, value );
        }

        private static void PropertyChangedCallback( AvaloniaObject d, AvaloniaPropertyChangedEventArgs e )
        {
            if ( !( d is CombineStacksSettingsControl control ) )
            {
                return;
            }

            if ( control.DataContext is CombineStacksSettingsViewModel vm )
            {
                vm.Items = (ObservableCollection<CombineStacksOpenContainersIgnoreEntry>) e.NewValue;
            }
        }
    }
}