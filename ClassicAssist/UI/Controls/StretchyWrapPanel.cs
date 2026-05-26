using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace ClassicAssist.UI.Controls
{
    /*
     * https://stackoverflow.com/questions/8004981/how-to-make-wpf-wrappanel-child-items-to-stretch
     */
    public class StretchyWrapPanel : Panel
    {
        public static readonly StyledProperty<double> ItemWidthProperty = AvaloniaProperty.Register<StretchyWrapPanel, double>( nameof( ItemWidth ), double.NaN );

        public static readonly StyledProperty<double> ItemHeightProperty = AvaloniaProperty.Register<StretchyWrapPanel, double>( nameof( ItemHeight ), double.NaN );

        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<StretchyWrapPanel, Orientation>( nameof( Orientation ), Orientation.Horizontal );

        public static readonly StyledProperty<bool> StretchProportionallyProperty = AvaloniaProperty.Register<StretchyWrapPanel, bool>( nameof( StretchProportionally ), true );

        static StretchyWrapPanel()
        {
            AffectsMeasure<StretchyWrapPanel>( ItemWidthProperty, ItemHeightProperty, OrientationProperty, StretchProportionallyProperty );
        }

        private Orientation _orientation = Orientation.Horizontal;

        private bool _stretchProportionally = true;

        private double _uniformLineV;

        public double ItemHeight
        {
            get => (double) GetValue( ItemHeightProperty );
            set => SetValue( ItemHeightProperty, value );
        }

        public double ItemWidth
        {
            get => (double) GetValue( ItemWidthProperty );
            set => SetValue( ItemWidthProperty, value );
        }

        public Orientation Orientation
        {
            get => _orientation;
            set => SetValue( OrientationProperty, value );
        }

        public bool StretchProportionally
        {
            get => _stretchProportionally;
            set => SetValue( StretchProportionallyProperty, value );
        }

        private static void OnOrientationChanged( AvaloniaObject d, AvaloniaPropertyChangedEventArgs e )
        {
            ( (StretchyWrapPanel) d )._orientation = (Orientation) e.NewValue;
        }

        private static void OnStretchProportionallyChanged( AvaloniaObject o, AvaloniaPropertyChangedEventArgs e )
        {
            ( (StretchyWrapPanel) o )._stretchProportionally = (bool) e.NewValue;
        }

        protected override Size MeasureOverride( Size constraint )
        {
            UVSize curLineSize = new UVSize( Orientation );
            UVSize panelSize = new UVSize( Orientation );
            UVSize uvConstraint = new UVSize( Orientation, constraint.Width, constraint.Height );
            double itemWidth = ItemWidth;
            double itemHeight = ItemHeight;
            bool itemWidthSet = !double.IsNaN( itemWidth );
            bool itemHeightSet = !double.IsNaN( itemHeight );

            Size childConstraint = new Size( itemWidthSet ? itemWidth : constraint.Width,
                itemHeightSet ? itemHeight : constraint.Height );

            var children = Children;

            for ( int i = 0, count = children.Count; i < count; i++ )
            {
                Control child = children[i];

                if ( child == null )
                {
                    continue;
                }

                // Flow passes its own constrint to children
                child.Measure( childConstraint );

                // This is the size of the child in UV space
                UVSize sz = new UVSize( Orientation, itemWidthSet ? itemWidth : child.DesiredSize.Width,
                    itemHeightSet ? itemHeight : child.DesiredSize.Height );

                if ( curLineSize.U + sz.U > uvConstraint.U )
                {
                    // Need to switch to another line
                    panelSize.U = Math.Max( curLineSize.U, panelSize.U );
                    panelSize.V += curLineSize.V;
                    curLineSize = sz;

                    if ( sz.U > uvConstraint.U )
                    {
                        // The element is wider then the constrint - give it a separate line             
                        panelSize.U = Math.Max( sz.U, panelSize.U );
                        panelSize.V += sz.V;
                        curLineSize = new UVSize( Orientation );
                    }
                }
                else
                {
                    // Continue to accumulate a line
                    curLineSize.U += sz.U;
                    curLineSize.V = Math.Max( sz.V, curLineSize.V );
                }
            }

            // The last line size, if any should be added
            panelSize.U = Math.Max( curLineSize.U, panelSize.U );
            panelSize.V += curLineSize.V;

            // Pre-compute the max line V across all lines so Arrange can give every
            // line the same height. WPF's WrapPanel arranges per-line height, but
            // ClassicAssist's button rows look uniform there because of WPF default
            // Button styling we don't have on Avalonia. Equalizing here is simpler
            // than chasing every default-style discrepancy.
            _uniformLineV = 0;
            UVSize lv = new UVSize( Orientation );
            double totalV = 0;
            int lineCount = 0;
            for ( int i = 0, count = children.Count; i < count; i++ )
            {
                var child = children[i];
                if ( child == null ) continue;
                UVSize sz = new UVSize( Orientation, itemWidthSet ? itemWidth : child.DesiredSize.Width, itemHeightSet ? itemHeight : child.DesiredSize.Height );
                if ( lv.U + sz.U > uvConstraint.U )
                {
                    totalV += lv.V; lineCount++;
                    _uniformLineV = Math.Max( _uniformLineV, lv.V );
                    lv = sz;
                }
                else { lv.U += sz.U; lv.V = Math.Max( lv.V, sz.V ); }
            }
            if ( lv.U > 0 ) { totalV += lv.V; lineCount++; _uniformLineV = Math.Max( _uniformLineV, lv.V ); }
            // Report panel height = uniform line V × line count so the parent gives us enough room.
            double uniformHeight = _uniformLineV * lineCount;

            // Go from UV space to W/H space
            return Orientation == Orientation.Horizontal
                ? new Size( panelSize.Width, uniformHeight )
                : new Size( uniformHeight, panelSize.Height );
        }

        protected override Size ArrangeOverride( Size finalSize )
        {
            int firstInLine = 0;
            double itemWidth = ItemWidth;
            double itemHeight = ItemHeight;
            double accumulatedV = 0;
            double itemU = Orientation == Orientation.Horizontal ? itemWidth : itemHeight;
            UVSize curLineSize = new UVSize( Orientation );
            UVSize uvFinalSize = new UVSize( Orientation, finalSize.Width, finalSize.Height );
            bool itemWidthSet = !double.IsNaN( itemWidth );
            bool itemHeightSet = !double.IsNaN( itemHeight );
            bool useItemU = Orientation == Orientation.Horizontal ? itemWidthSet : itemHeightSet;

            var children = Children;

            for ( int i = 0, count = children.Count; i < count; i++ )
            {
                Control child = children[i];

                if ( child == null )
                {
                    continue;
                }

                UVSize sz = new UVSize( Orientation, itemWidthSet ? itemWidth : child.DesiredSize.Width,
                    itemHeightSet ? itemHeight : child.DesiredSize.Height );

                if ( curLineSize.U + sz.U > uvFinalSize.U )
                {
                    // Need to switch to another line
                    if ( !useItemU && StretchProportionally )
                    {
                        ArrangeLineProportionally( accumulatedV, _uniformLineV > 0 ? _uniformLineV : curLineSize.V, firstInLine, i, uvFinalSize.Width );
                    }
                    else
                    {
                        ArrangeLine( accumulatedV, _uniformLineV > 0 ? _uniformLineV : curLineSize.V, firstInLine, i, true,
                            useItemU ? itemU : uvFinalSize.Width / Math.Max( 1, i - firstInLine - 1 ) );
                    }

                    accumulatedV += ( _uniformLineV > 0 ? _uniformLineV : curLineSize.V );
                    curLineSize = sz;

                    if ( sz.U > uvFinalSize.U )
                    {
                        // The element is wider then the constraint - give it a separate line
                        // Switch to next line which only contain one element
                        double lineV = _uniformLineV > 0 ? _uniformLineV : sz.V;
                        if ( !useItemU && StretchProportionally )
                        {
                            ArrangeLineProportionally( accumulatedV, lineV, i, ++i, uvFinalSize.Width );
                        }
                        else
                        {
                            ArrangeLine( accumulatedV, lineV, i, ++i, true, useItemU ? itemU : uvFinalSize.Width );
                        }

                        accumulatedV += lineV;
                        curLineSize = new UVSize( Orientation );
                    }

                    firstInLine = i;
                }
                else
                {
                    // Continue to accumulate a line
                    curLineSize.U += sz.U;
                    curLineSize.V = Math.Max( sz.V, curLineSize.V );
                }
            }

            // Arrange the last line, if any
            if ( firstInLine < children.Count )
            {
                if ( !useItemU && StretchProportionally )
                {
                    ArrangeLineProportionally( accumulatedV, _uniformLineV > 0 ? _uniformLineV : curLineSize.V, firstInLine, children.Count,
                        uvFinalSize.Width );
                }
                else
                {
                    ArrangeLine( accumulatedV, _uniformLineV > 0 ? _uniformLineV : curLineSize.V, firstInLine, children.Count, true,
                        useItemU ? itemU : uvFinalSize.Width / Math.Max( 1, children.Count - firstInLine - 1 ) );
                }
            }

            return finalSize;
        }

        private void ArrangeLineProportionally( double v, double lineV, int start, int end, double limitU )
        {
            double u = 0d;
            bool horizontal = Orientation == Orientation.Horizontal;
            var children = Children;

            double total = 0d;

            for ( int i = start; i < end; i++ )
            {
                total += horizontal ? children[i].DesiredSize.Width : children[i].DesiredSize.Height;
            }

            double uMultipler = limitU / total;

            for ( int i = start; i < end; i++ )
            {
                Control child = children[i];

                if ( child != null )
                {
                    UVSize childSize = new UVSize( Orientation, child.DesiredSize.Width, child.DesiredSize.Height );
                    double layoutSlotU = childSize.U * uMultipler;
                    child.Arrange( new Rect( horizontal ? u : v, horizontal ? v : u, horizontal ? layoutSlotU : lineV,
                        horizontal ? lineV : layoutSlotU ) );
                    u += layoutSlotU;
                }
            }
        }

        private void ArrangeLine( double v, double lineV, int start, int end, bool useItemU, double itemU )
        {
            double u = 0d;
            bool horizontal = Orientation == Orientation.Horizontal;
            var children = Children;

            for ( int i = start; i < end; i++ )
            {
                Control child = children[i];

                if ( child != null )
                {
                    UVSize childSize = new UVSize( Orientation, child.DesiredSize.Width, child.DesiredSize.Height );
                    double layoutSlotU = useItemU ? itemU : childSize.U;
                    child.Arrange( new Rect( horizontal ? u : v, horizontal ? v : u, horizontal ? layoutSlotU : lineV,
                        horizontal ? lineV : layoutSlotU ) );
                    u += layoutSlotU;
                }
            }
        }

        private struct UVSize
        {
            internal UVSize( Orientation orientation, double width, double height )
            {
                U = V = 0d;
                _orientation = orientation;
                Width = width;
                Height = height;
            }

            internal UVSize( Orientation orientation )
            {
                U = V = 0d;
                _orientation = orientation;
            }

            internal double U;
            internal double V;
            private readonly Orientation _orientation;

            internal double Width
            {
                get => _orientation == Orientation.Horizontal ? U : V;
                private set
                {
                    if ( _orientation == Orientation.Horizontal )
                    {
                        U = value;
                    }
                    else
                    {
                        V = value;
                    }
                }
            }

            internal double Height
            {
                get => _orientation == Orientation.Horizontal ? V : U;
                private set
                {
                    if ( _orientation == Orientation.Horizontal )
                    {
                        V = value;
                    }
                    else
                    {
                        U = value;
                    }
                }
            }
        }
    }
}