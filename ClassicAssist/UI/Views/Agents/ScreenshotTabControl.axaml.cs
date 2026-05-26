using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClassicAssist.UI.ViewModels.Agents;
// Copyright (C) 2023 Reetus
//  
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

namespace ClassicAssist.UI.Views.Agents
{
    /// <summary>
    ///     Interaction logic for ScreenshotTabControl.xaml
    /// </summary>
    public partial class ScreenshotTabControl : UserControl
    {
        public ScreenshotTabControl()
        {
            InitializeComponent();
        }

        private void ScreenshotItem_OnDoubleTapped( object sender, TappedEventArgs e )
        {
            if ( !( sender is Control c ) || !( DataContext is ScreenshotTabViewModel vm ) )
            {
                return;
            }

            if ( vm.OpenScreenshotCommand?.CanExecute( c.DataContext ) == true )
            {
                vm.OpenScreenshotCommand.Execute( c.DataContext );
            }
        }
    }
}