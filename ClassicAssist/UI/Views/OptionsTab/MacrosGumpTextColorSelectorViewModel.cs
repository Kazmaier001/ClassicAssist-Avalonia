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
using Avalonia.Input;
using System.Windows.Input;
using Avalonia.Media;
using ClassicAssist.Shared.UI;

namespace ClassicAssist.UI.Views.OptionsTab
{
    public class MacrosGumpTextColorSelectorViewModel : SetPropertyNotifyChanged
    {
        private bool _allowAlpha;
        private ICommand _okCommand;
        private Color _selectedColor = Colors.White;

        public bool AllowAlpha
        {
            get => _allowAlpha;
            set => SetProperty( ref _allowAlpha, value );
        }

        public ICommand OKCommand => _okCommand ?? ( _okCommand = new RelayCommand( OK ) );
        public bool Result { get; set; }

        // Code-behind subscribes and calls Window.Close(). Avalonia's Click
        // fires BEFORE Command.Execute (opposite of WPF) — see the memory
        // pin avalonia-click-vs-command-order — so a CloseOnClickBehaviour
        // on the OK button would shut the window down before OK() can set
        // Result. Closing from inside the command is the atomic fix.
        public event Action RequestClose;

        public Color SelectedColor
        {
            get => _selectedColor;
            set => SetProperty( ref _selectedColor, value );
        }

        private void OK( object obj )
        {
            Result = true;
            RequestClose?.Invoke();
        }
    }
}