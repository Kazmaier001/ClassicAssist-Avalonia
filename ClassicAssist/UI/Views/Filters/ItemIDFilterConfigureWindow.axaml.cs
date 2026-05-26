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
using Avalonia.Controls;
using ClassicAssist.Misc;

namespace ClassicAssist.UI.Views.Filters
{
    /// <summary>
    ///     Interaction logic for ItemIDFilterConfigureWindow.xaml
    /// </summary>
    public partial class ItemIDFilterConfigureWindow : Window
    {
        public ItemIDFilterConfigureWindow()
        {
            InitializeComponent();

            // Avalonia BindingProxy doesn't inherit DataContext through Resources;
            // wire Proxy.Data explicitly so source/destination/hue cell commands
            // resolve to the VM.
            DataContextChanged += ( s, e ) => SyncProxyData();
            SyncProxyData();
        }

        private void SyncProxyData()
        {
            if ( Resources.TryGetValue( "Proxy", out object proxyObj ) && proxyObj is BindingProxy proxy )
            {
                proxy.Data = DataContext;
            }
        }
    }
}