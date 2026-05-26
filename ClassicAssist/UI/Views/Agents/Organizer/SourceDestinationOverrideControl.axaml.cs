// Copyright (C) 2024 Reetus
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
using Avalonia.Input;
using System.Windows.Input;

namespace ClassicAssist.UI.Views.Agents.Organizer
{
    /// <summary>
    ///     Interaction logic for SourceDestinationOverrideControl.xaml
    /// </summary>
    public partial class SourceDestinationOverrideControl : UserControl
    {
        public static readonly StyledProperty<int?> SerialProperty = AvaloniaProperty.Register<SourceDestinationOverrideControl, int?>( nameof( Serial ) );

        public static readonly StyledProperty<ICommand> SetCommandProperty = AvaloniaProperty.Register<SourceDestinationOverrideControl, ICommand>( nameof( SetCommand ) );

        public static readonly StyledProperty<object> SetCommandParameterProperty = AvaloniaProperty.Register<SourceDestinationOverrideControl, object>( nameof( SetCommandParameter ) );

        public static readonly StyledProperty<ICommand> ClearCommandProperty = AvaloniaProperty.Register<SourceDestinationOverrideControl, ICommand>( nameof( ClearCommand ) );

        public SourceDestinationOverrideControl()
        {
            InitializeComponent();
        }

        public ICommand ClearCommand
        {
            get => (ICommand) GetValue( ClearCommandProperty );
            set => SetValue( ClearCommandProperty, value );
        }

        public int? Serial
        {
            get => (int?) GetValue( SerialProperty );
            set => SetValue( SerialProperty, value );
        }

        public ICommand SetCommand
        {
            get => (ICommand) GetValue( SetCommandProperty );
            set => SetValue( SetCommandProperty, value );
        }

        public object SetCommandParameter
        {
            get => GetValue( SetCommandParameterProperty );
            set => SetValue( SetCommandParameterProperty, value );
        }
    }
}