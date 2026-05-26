// Copyright (C) 2024 Reetus
//  
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;

namespace ClassicAssist.UI.Views.Agents.Autoloot.Import
{
    /// <summary>
    ///     Interaction logic for CSVImportWindow.xaml
    /// </summary>
    public partial class CSVImportWindow : Window
    {
        public CSVImportWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_OnRequestNavigate( object sender, Avalonia.Input.PointerReleasedEventArgs e )
        {
            if ( sender is Control control && control.Tag is string url )
            {
                Process.Start( new ProcessStartInfo( url ) { UseShellExecute = true } );
            }
        }
    }
}