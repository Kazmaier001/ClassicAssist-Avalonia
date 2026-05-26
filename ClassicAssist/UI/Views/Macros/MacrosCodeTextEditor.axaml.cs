#region License

// Copyright (C) 2025 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using System.Windows.Input;
using System.Xml;
using Assistant;
using ClassicAssist.Data.Macros;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Search;

namespace ClassicAssist.UI.Views.Macros
{
    /// <summary>
    ///     Interaction logic for MacrosCodeTextEditor.xaml
    /// </summary>
    public partial class MacrosCodeTextEditor : UserControl
    {
        public static readonly StyledProperty<string> FormatErrorProperty = AvaloniaProperty.Register<MacrosCodeTextEditor, string>( nameof( FormatError ) );

        public static readonly StyledProperty<bool> IsPerformingActionProperty = AvaloniaProperty.Register<MacrosCodeTextEditor, bool>( nameof( IsPerformingAction ) );

        public static readonly StyledProperty<ICommand> ClearExceptionCommandProperty = AvaloniaProperty.Register<MacrosCodeTextEditor, ICommand>( nameof( ClearExceptionCommand ) );

        // TwoWay so AvalonBindingBehaviour can push the live AvaloniaEdit
        // TextDocument and caret offset back up into MacrosTabViewModel.
        // Without TwoWay, VM.Document stays null and the Commands-window
        // "Insert" button NREs on the first click. See [[avalonia-two-way-binding]].
        public static readonly StyledProperty<int> CaretPositionProperty = AvaloniaProperty.Register<MacrosCodeTextEditor, int>( nameof( CaretPosition ), defaultBindingMode: BindingMode.TwoWay );

        public static readonly StyledProperty<TextDocument> DocumentProperty = AvaloniaProperty.Register<MacrosCodeTextEditor, TextDocument>( nameof( Document ), defaultBindingMode: BindingMode.TwoWay );

        public static readonly StyledProperty<MacroEntry> SelectedItemProperty = AvaloniaProperty.Register<MacrosCodeTextEditor, MacroEntry>( nameof( SelectedItem ) );

        public MacrosCodeTextEditor()
        {
            InitializeComponent();
        }

        public int CaretPosition
        {
            get => (int) GetValue( CaretPositionProperty );
            set => SetValue( CaretPositionProperty, value );
        }

        public ICommand ClearExceptionCommand
        {
            get => (ICommand) GetValue( ClearExceptionCommandProperty );
            set => SetValue( ClearExceptionCommandProperty, value );
        }

        public TextDocument Document
        {
            get => (TextDocument) GetValue( DocumentProperty );
            set => SetValue( DocumentProperty, value );
        }

        public string FormatError
        {
            get => (string) GetValue( FormatErrorProperty );
            set => SetValue( FormatErrorProperty, value );
        }

        public bool IsPerformingAction
        {
            get => (bool) GetValue( IsPerformingActionProperty );
            set => SetValue( IsPerformingActionProperty, value );
        }

        public MacroEntry SelectedItem
        {
            get => (MacroEntry) GetValue( SelectedItemProperty );
            set => SetValue( SelectedItemProperty, value );
        }

        private void Grid_Initialized( object sender, EventArgs e )
        {
            var editor = this.FindControl<AvaloniaEdit.TextEditor>( "CodeTextEditor" );
            if ( editor != null )
            {
                editor.SyntaxHighlighting = HighlightingLoader.Load(
                    new XmlTextReader( Path.Combine( Engine.StartupPath, "Python.Dark.xshd" ) ), HighlightingManager.Instance );
                SearchPanel.Install( editor );
            }
        }
    }
}