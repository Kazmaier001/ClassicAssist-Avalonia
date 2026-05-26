using System;
using ClassicAssist.Misc;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Input;
using System.Windows.Input;
using ClassicAssist.Annotations;
using ClassicAssist.Shared.UI;
using AvaloniaEdit.Highlighting;
using Avalonia.Controls;
namespace ClassicAssist.UI.Controls
{
    /// <summary>
    ///     Interaction logic for CompletionEntry.xaml
    /// </summary>
    public partial class CompletionEntry : UserControl, INotifyPropertyChanged
    {
        private ICommand _copyToClipboardCommand;
        private string _entryExample;
        private string _entryName;
        private bool _isButtonEnabled;
        private bool _isExpanded;
        private ICommand _toggleExpandedCommand;

        public CompletionEntry()
        {
            InitializeComponent();
        }

        public CompletionEntry( string entryName, string entryExample, IHighlightingDefinition highlightingDefinition )
        {
            InitializeComponent();
            EntryName = entryName;
            EntryExample = entryExample;
            IsButtonEnabled = !string.IsNullOrEmpty( EntryExample );
            var editor = this.FindControl<AvaloniaEdit.TextEditor>( "CodeTextEditor" );
            if ( editor != null ) editor.SyntaxHighlighting = highlightingDefinition;
        }

        public ICommand CopyToClipboardCommand => _copyToClipboardCommand ?? ( _copyToClipboardCommand = new RelayCommand( CopyToClipboard, o => true ) );

        public string EntryExample
        {
            get => _entryExample;
            set
            {
                _entryExample = value;
                OnPropertyChanged();
            }
        }

        public string EntryName
        {
            get => _entryName;
            set
            {
                _entryName = value;
                OnPropertyChanged();
            }
        }

        public bool IsButtonEnabled
        {
            get => _isButtonEnabled;
            set
            {
                _isButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public ICommand ToggleExpandedCommand => _toggleExpandedCommand ?? ( _toggleExpandedCommand = new RelayCommand( ToggleExpanded, o => true ) );

        public event PropertyChangedEventHandler PropertyChanged;

        private void ToggleExpanded( object obj )
        {
            IsExpanded = !IsExpanded;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged( [CallerMemberName] string propertyName = null )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

        private static void CopyToClipboard( object obj )
        {
            if ( !( obj is string macro ) )
            {
                return;
            }

            try
            {
                ClipboardCompat.SetText( macro );
            }
            catch ( Exception )
            {
                // ignored
            }
        }
    }
}