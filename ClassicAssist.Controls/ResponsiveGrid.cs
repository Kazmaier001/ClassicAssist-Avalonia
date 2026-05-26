#region License

// Copyright (C) $CURRENT_YEAR$ Reetus
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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace ClassicAssist.Controls
{
    // Avalonia port of the WPF ResponsiveGrid from
    // Source/ClassicAssist.Controls/ResponsiveGrid.cs. The WPF algorithm:
    //  Phase 1 — walk items in order. Whenever the running column height
    //    exceeds availableHeight, start a new column (incrementing the
    //    column count). If we'd need more columns than width allows
    //    (availableWidth / (columns+1) < _minWidth), set wontFit=true and
    //    skip the increment.
    //  Phase 2 — distribute items into the resulting column count. Col 0
    //    greedily fills up to availableHeight; subsequent cols continue
    //    with the leftover. The last col takes everything still unplaced.
    //  When wontFit, wrap each column in a ScrollViewer.
    //
    // Avalonia notes (lessons learned):
    //  * Items live in a List<Control> property — they are NOT part of the
    //    logical/visual tree until UpdateItems re-parents them into the
    //    per-column StackPanels. The XAML order is preserved via List
    //    iteration; HashSet was used in the WPF original but its ordering
    //    is implementation-defined in .NET and was flipping the deterministic
    //    Col0/Col1 distribution.
    //  * Inside a ScrollViewer, `Bounds.Height` is the natural content height
    //    (not viewport). To match the WPF "viewport drives wraps" behaviour
    //    we walk up to find the ancestor ScrollViewer and use its Bounds.Height.
    //  * BoundsProperty observer fires when we rebuild Children inside
    //    UpdateItems — so we both guard re-entrance AND skip if the layout
    //    inputs haven't materially changed since the last successful run.
    //    Without the change-detect skip, boundary widths oscillate between
    //    targetCols=N and N+1 because items in narrower cols measure taller
    //    than items in wider cols, which is exactly what feeds back into
    //    the column count calculation.
    public class ResponsiveGrid : Grid
    {
        private bool _loaded;
        private bool _updating;
        private bool _updatePending;
        private double _cachedMinWidth;
        private int _lastColumns = -1;
        private double _lastAvailWidth = -1;
        private double _lastViewportHeight = -1;

        public ResponsiveGrid()
        {
            this.GetObservable( BoundsProperty ).Subscribe( new AnonymousObserver<Rect>( _ =>
            {
                if ( _loaded ) ScheduleUpdate();
            } ) );

            // Loaded (NOT AttachedToVisualTree) — fires AFTER the initial layout
            // pass, so items have real DesiredSize values. AttachedToVisualTree
            // ran too early, computed cols=1 with unmeasured items, then a
            // deferred re-run flipped to cols=2, producing a visible flash.
            // WPF's Loaded event has the same post-layout semantics.
            Loaded += OnLoaded;
        }

        public List<Control> Items { get; set; } = new List<Control>();

        private void OnLoaded( object sender, RoutedEventArgs e )
        {
            // Pre-attach all items into a single hidden StackPanel and force
            // a layout pass so every item gets its template applied and its
            // DesiredSize populated *before* phase 1 counts columns. Without
            // this priming step, items measure off the visual tree, return
            // near-zero heights, phase 1 picks columns=1, and the user sees a
            // brief one-column layout before a deferred re-run flips to two.
            // The host StackPanel is laid out at zero opacity so even the
            // intermediate single-column arrangement is never drawn.
            if ( Items.Count > 0 && Children.Count == 0 )
            {
                StackPanel primer = new StackPanel { Opacity = 0 };
                ColumnDefinitions.Add( new ColumnDefinition() );
                foreach ( Control element in Items )
                {
                    var parent = element.GetVisualParent();
                    if ( parent is Panel panel ) panel.Children.Remove( element );
                    primer.Children.Add( element );
                }
                Children.Add( primer );
                UpdateLayout();
            }

            UpdateItems();
            _loaded = true;
        }

        // Defer UpdateItems to after the current layout pass completes. Direct
        // synchronous invocation in the BoundsProperty observer fires DURING
        // layout, before the ancestor ScrollViewer's Bounds.Height has settled —
        // so on window maximize → restore we'd read the stale (maximized)
        // viewport height and compute too few columns. Symptom: left column too
        // tall + no scroll bar appears. DispatcherPriority.Loaded fires after
        // measure/arrange. The _updatePending guard coalesces a burst of bounds
        // events (one per intermediate animation frame during a window resize)
        // into a single post-layout update.
        private void ScheduleUpdate()
        {
            if ( _updatePending ) return;
            _updatePending = true;
            Avalonia.Threading.Dispatcher.UIThread.Post(
                () =>
                {
                    _updatePending = false;
                    UpdateItems();
                },
                Avalonia.Threading.DispatcherPriority.Loaded );
        }

        // Per-item natural width. Measured once, when items first have
        // non-zero sizes available. WPF computed this as max(item.DesiredSize.Width)+10
        // over Items, gated by the XAML MinWidth as a floor.
        private double GetMinimumWidth()
        {
            if ( _cachedMinWidth > 0 ) return _cachedMinWidth;

            double minWidth = MinWidth;
            bool anyMeasured = false;
            foreach ( Control element in Items )
            {
                element.Measure( new Size( double.PositiveInfinity, double.PositiveInfinity ) );
                if ( element.DesiredSize.Width > 0 ) anyMeasured = true;
                if ( element.DesiredSize.Width > minWidth )
                    minWidth = element.DesiredSize.Width + 10;
            }
            // Don't cache until items have a real measurement — the very first
            // call (before initial layout) sees DesiredSize=0 and would lock
            // us into MinWidth forever.
            if ( anyMeasured ) _cachedMinWidth = minWidth;
            return minWidth;
        }

        private void UpdateItems()
        {
            if ( _updating ) return;

            double availableWidth = Bounds.Width;
            if ( availableWidth <= 0 ) return;

            // Viewport height — find the ancestor ScrollViewer if any. WPF
            // got this for free because ActualHeight inside a ScrollViewer
            // was the viewport. Avalonia's Bounds.Height is the natural
            // content height.
            double availableHeight = Bounds.Height;
            var cursor = this.GetVisualParent();
            while ( cursor != null )
            {
                if ( cursor is ScrollViewer sv )
                {
                    if ( sv.Bounds.Height > 0 ) availableHeight = sv.Bounds.Height;
                    break;
                }
                cursor = cursor.GetVisualParent();
            }
            if ( availableHeight <= 0 ) return;

            List<Control> items = Items.ToList();
            if ( items.Count == 0 ) return;

            double minWidth = GetMinimumWidth();

            // Measure items at the available width (constrained). This is
            // what WPF's algorithm does — items in the un-wrapped 1-column
            // layout. Heights computed here drive phase-1 col counting.
            Dictionary<int, Size> measuredSizes = new Dictionary<int, Size>( items.Count );
            foreach ( Control element in items )
            {
                int hash = RuntimeHelpers.GetHashCode( element );
                element.Measure( new Size( availableWidth, availableHeight ) );
                measuredSizes[hash] = element.DesiredSize;
            }

            // Phase 1 — count columns by per-item overflow. Matches WPF.
            int columns = 1;
            bool wontFit = false;
            double currentHeight = 0;
            foreach ( Control element in items )
            {
                double h = measuredSizes[RuntimeHelpers.GetHashCode( element )].Height;
                if ( currentHeight + h > availableHeight )
                {
                    if ( availableWidth / ( columns + 1 ) < minWidth )
                    {
                        wontFit = true;
                        continue;
                    }
                    columns++;
                    currentHeight = 0;
                }
                currentHeight += h;
            }

            // Phase 2 cap: WPF uses availableHeight directly when not wontFit;
            // when wontFit, it reduces availableHeight to totalHeight/columns
            // so the last col doesn't visually drift far from the others.
            double perColHeight = availableHeight;
            if ( wontFit )
            {
                double totalHeight = items.Sum( x => measuredSizes[RuntimeHelpers.GetHashCode( x )].Height );
                perColHeight = Math.Min( availableHeight, totalHeight / columns );
            }

            // Skip the rebuild if nothing has changed materially. This stops
            // the post-rebuild Bounds observer from re-running UpdateItems
            // with the same inputs (which would oscillate between layouts
            // because items in narrow vs wide cols measure to different
            // heights, flipping the column count back and forth).
            if ( columns == _lastColumns
                 && Math.Abs( availableWidth - _lastAvailWidth ) < 0.5
                 && Math.Abs( availableHeight - _lastViewportHeight ) < 0.5 )
            {
                return;
            }
            _lastColumns = columns;
            _lastAvailWidth = availableWidth;
            _lastViewportHeight = availableHeight;

            _updating = true;
            try
            {
                Children.Clear();
                ColumnDefinitions.Clear();
                InvalidateMeasure();
                InvalidateArrange();

                HashSet<Control> remaining = new HashSet<Control>( items );

                for ( int i = 0; i < columns; i++ )
                {
                    if ( remaining.Count == 0 ) break;

                    currentHeight = 0;
                    StackPanel stackPanel = new StackPanel();
                    ColumnDefinitions.Add( new ColumnDefinition() );

                    if ( wontFit )
                    {
                        // Pin each per-col ScrollViewer to the viewport height
                        // so it actually engages — otherwise the outer
                        // ScrollViewer (in OptionsTabControl.axaml) gives it
                        // infinite available height, no inner scroll bar
                        // appears, and the outer one ends up scrolling
                        // everything together (left cols slide with right).
                        // VerticalScrollBarVisibility=Auto means only the
                        // overflowing column shows its bar.
                        ScrollViewer scrollViewer = new ScrollViewer
                        {
                            Content = stackPanel,
                            Height = availableHeight,
                            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                            // Always-visible WPF-style bars (no auto-hide on idle) —
                            // matches the Macros tab scrollers.
                            AllowAutoHide = false
                        };
                        SetColumn( scrollViewer, i );
                        Children.Add( scrollViewer );
                    }
                    else
                    {
                        SetColumn( stackPanel, i );
                        Children.Add( stackPanel );
                    }

                    int row = 0;
                    // Iterate in original XAML order to preserve column packing.
                    foreach ( Control element in items )
                    {
                        if ( !remaining.Contains( element ) ) continue;
                        int hash = RuntimeHelpers.GetHashCode( element );
                        double h = measuredSizes[hash].Height;

                        if ( ( !wontFit || i + 1 < columns ) && currentHeight + h > perColHeight )
                            continue;

                        if ( i + 1 < columns )
                            element.Margin = new Thickness( 0, row != 0 ? 5 : 0, 5, 0 );
                        else if ( row != 0 )
                            element.Margin = new Thickness( 0, 5, 0, 0 );
                        else
                            element.Margin = new Thickness( 0, 0, 0, 0 );

                        currentHeight += h;

                        var parent = element.GetVisualParent();
                        if ( parent is Panel panel ) panel.Children.Remove( element );

                        stackPanel.Children.Add( element );
                        remaining.Remove( element );
                        row++;
                    }
                }
            }
            finally
            {
                _updating = false;
            }
        }
    }
}
