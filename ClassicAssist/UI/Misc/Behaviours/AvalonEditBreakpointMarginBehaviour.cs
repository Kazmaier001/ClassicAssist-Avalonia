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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.UI.Misc.Behaviours
{
    public class AvalonEditBreakpointMarginBehaviour : Behavior<TextEditor>
    {
        public static readonly AttachedProperty<ObservableCollection<int>> BreakpointsProperty =
            AvaloniaProperty.RegisterAttached<AvalonEditBreakpointMarginBehaviour, AvaloniaObject, ObservableCollection<int>>( "Breakpoints" );

        static AvalonEditBreakpointMarginBehaviour()
        {
            // WPF wired this via DependencyProperty.RegisterAttached's metadata
            // callback; Avalonia's RegisterAttached overload has no callback
            // slot, so do it explicitly here or AddBreakpointMargin never runs.
            BreakpointsProperty.Changed.AddClassHandler<AvalonEditBreakpointMarginBehaviour>(
                ( behavior, e ) => OnBreakpointsChanged( behavior, e ) );
        }

        private BreakpointMargin _breakPointMargin;
        private TextEditor _textEditor;
        private TextDocument _subscribedDocument;

        public static void SetBreakpoints( AvaloniaObject element, ObservableCollection<int> value )
        {
            element?.SetValue( BreakpointsProperty, value );
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if ( AssociatedObject != null )
            {
                _textEditor = AssociatedObject;
                _textEditor.Loaded += OnEditorLoaded;

                // The Breakpoints binding may have evaluated before OnAttached
                // ran, in which case OnBreakpointsChanged was a no-op because
                // _textEditor was still null. Catch up here.
                ObservableCollection<int> current = (ObservableCollection<int>) GetValue( BreakpointsProperty );

                if ( current != null && _breakPointMargin == null )
                {
                    AddBreakpointMargin( current );
                }
            }
        }

        protected override void OnDetaching()
        {
            if ( _textEditor != null )
            {
                _textEditor.Loaded -= OnEditorLoaded;
            }

            if ( _subscribedDocument != null )
            {
                _subscribedDocument.Changed -= Document_Changed;
                _subscribedDocument = null;
            }

            base.OnDetaching();
        }

        private void OnEditorLoaded( object sender, RoutedEventArgs e )
        {
            if ( _textEditor == null )
            {
                return;
            }

            TextDocument doc = _textEditor.Document;

            if ( ReferenceEquals( _subscribedDocument, doc ) )
            {
                return;
            }

            if ( _subscribedDocument != null )
            {
                _subscribedDocument.Changed -= Document_Changed;
                _subscribedDocument = null;
            }

            if ( doc != null )
            {
                _subscribedDocument = doc;
                doc.Changed += Document_Changed;
            }
        }

        private void Document_Changed( object sender, DocumentChangeEventArgs e )
        {
            TextDocument doc = _textEditor.Document;
            int startLine = doc.GetLineByOffset( e.Offset ).LineNumber;

            int lineDelta = 0;

            if ( e.InsertionLength == doc.TextLength )
            {
                return;
            }

            if ( e.InsertionLength > 0 )
            {
                // Count inserted newlines
                string insertedText = e.InsertedText.Text;
                lineDelta = insertedText.Count( c => c == '\n' );

                if ( lineDelta > 0 )
                {
                    ShiftBreakpoints( startLine, lineDelta );
                }
            }
            else if ( e.RemovalLength > 0 )
            {
                // Count removed newlines
                string removedText = e.RemovedText.Text;
                lineDelta = -removedText.Count( c => c == '\n' );

                if ( lineDelta < 0 )
                {
                    ShiftBreakpoints( startLine, lineDelta );
                }
            }
        }

        private void ShiftBreakpoints( int fromLine, int delta )
        {
            if ( delta == 0 )
            {
                return;
            }

            ObservableCollection<int> breakpoints = (ObservableCollection<int>) GetValue( BreakpointsProperty );

            if ( breakpoints == null )
            {
                return;
            }

            foreach ( int bp in breakpoints.ToList().Where( bp => bp > fromLine ) )
            {
                breakpoints.Remove( bp );
                breakpoints.Add( bp + delta );
            }

            List<int> sortedBreakpoints = breakpoints.OrderBy( x => x ).ToList();

            breakpoints.Clear();

            foreach ( int bp in sortedBreakpoints )
            {
                breakpoints.Add( bp );
            }
        }

        private static void OnBreakpointsChanged( AvaloniaObject d, AvaloniaPropertyChangedEventArgs e )
        {
            if ( !( d is AvalonEditBreakpointMarginBehaviour editor ) )
            {
                return;
            }

            if ( e.OldValue != null )
            {
                editor.RemoveBreakpointMargin();
            }

            if ( e.NewValue != null )
            {
                editor.AddBreakpointMargin( (ObservableCollection<int>) e.NewValue );
            }
        }

        private void AddBreakpointMargin( ObservableCollection<int> breakpoints )
        {
            if ( breakpoints == null || _textEditor == null )
            {
                return;
            }

            BreakpointMargin margin = new BreakpointMargin { Breakpoints = breakpoints };
            _textEditor.TextArea.LeftMargins.Insert( 0, margin );
            _breakPointMargin = margin;

            if ( _breakPointMargin.Breakpoints != null )
            {
                _breakPointMargin.Breakpoints.CollectionChanged += Breakpoints_CollectionChanged;
            }
        }

        private void Breakpoints_CollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
        {
            SetValue( BreakpointsProperty, (ObservableCollection<int>) sender );
        }

        private void RemoveBreakpointMargin()
        {
            TextEditor editor = _textEditor;

            for ( int i = editor.TextArea.LeftMargins.Count - 1; i >= 0; i-- )
            {
                if ( editor.TextArea.LeftMargins[i] is BreakpointMargin )
                {
                    editor.TextArea.LeftMargins.RemoveAt( i );
                }
            }

            if ( _breakPointMargin == null )
            {
                return;
            }

            _breakPointMargin.Breakpoints.CollectionChanged -= Breakpoints_CollectionChanged;
            _breakPointMargin = null;
        }
    }
}