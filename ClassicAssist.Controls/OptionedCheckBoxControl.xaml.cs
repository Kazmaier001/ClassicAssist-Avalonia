// Copyright (C) 2023 Reetus
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
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace ClassicAssist.Controls
{
    /// <summary>
    ///     Interaction logic for OptionedCheckBoxControl.axaml
    /// </summary>
    public partial class OptionedCheckBoxControl : CheckBox
    {
        // Without this, Avalonia's Selector="CheckBox" doesn't match this
        // subclass — our DarkTheme template (and FluentTheme's) silently
        // skip the control and it renders without our re-templated box.
        protected override System.Type StyleKeyOverride => typeof( CheckBox );

        public static readonly StyledProperty<object> ChildContentProperty =
            AvaloniaProperty.Register<OptionedCheckBoxControl, object>( nameof( ChildContent ) );

        public OptionedCheckBoxControl()
        {
            InitializeComponent();

            this.GetObservable( ToggleButton.IsCheckedProperty ).Subscribe( new AnonymousObserver<bool?>( _ => OnCheckedChanged() ) );
        }

        static OptionedCheckBoxControl()
        {
            ChildContentProperty.Changed.AddClassHandler<OptionedCheckBoxControl>(
                ( c, _ ) => c.UpdateContent() );
        }

        public object ChildContent
        {
            get => GetValue( ChildContentProperty );
            set => SetValue( ChildContentProperty, value );
        }

        private void UpdateContent()
        {
            // WPF version: checkbox text + child content sit side-by-side
            // left-aligned (DockPanel with Dock=Left + LastChildFill child
            // anchored to its own width, not stretched).
            // The previous Avalonia port set HorizontalAlignment.Stretch on
            // the child-content presenter and Dock=Right, which combined
            // with LastChildFill=true left the child stretched across the
            // remaining row — fine for content that's itself a horizontal
            // StackPanel (which left-aligns its inner children) but for a
            // bare TextBox (like "Use object queue") the textbox ended up
            // centered/floated in the empty space rather than tucked next
            // to the checkbox label. Anchor the child presenter to the
            // left of the remaining space so it follows the checkbox text.
            DockPanel dockPanel = new DockPanel { LastChildFill = false };
            ContentPresenter checkBoxPresenter =
                new ContentPresenter { Content = Content, VerticalAlignment = VerticalAlignment.Center };
            DockPanel.SetDock( checkBoxPresenter, Dock.Left );
            dockPanel.Children.Add( checkBoxPresenter );

            ContentPresenter childContentPresenter = new ContentPresenter
            {
                Content = ChildContent,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            DockPanel.SetDock( childContentPresenter, Dock.Left );

            dockPanel.Children.Add( childContentPresenter );

            Content = dockPanel;

            OnCheckedChanged();
        }

        private void OnCheckedChanged()
        {
            if ( ChildContent is Control childElement )
            {
                childElement.IsEnabled = IsChecked ?? false;
            }
        }
    }

    internal class AnonymousObserver<T> : System.IObserver<T>
    {
        private readonly System.Action<T> _onNext;
        public AnonymousObserver( System.Action<T> onNext ) => _onNext = onNext;
        public void OnCompleted() { }
        public void OnError( System.Exception error ) { }
        public void OnNext( T value ) => _onNext( value );
    }
}