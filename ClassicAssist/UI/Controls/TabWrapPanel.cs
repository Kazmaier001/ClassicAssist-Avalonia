using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace ClassicAssist.UI.Controls
{
    /// <summary>
    /// Layout panel that mimics WPF's <c>System.Windows.Controls.Primitives.TabPanel</c>:
    /// <para>
    /// While the children fit on a single row at their natural widths, lays them
    /// out horizontally left-to-right at those natural widths.
    /// </para>
    /// <para>
    /// When the row would overflow, splits the children across the smallest number
    /// of rows N such that each row of ceil(count / N) children fits in the
    /// available width. Within a row, each child gets a share of the available
    /// width proportional to its natural width (so a long tab like "Public Macros"
    /// stays wider than a short tab like "Skills").
    /// </para>
    /// <para>
    /// When a child is selected (TabItem.IsSelected) and lives in a non-bottom
    /// row, the row containing the selected child is moved to the bottom so that
    /// its missing-bottom-border tab can visually merge with the content panel.
    /// This matches WPF's row-swap behaviour.
    /// </para>
    /// <para>
    /// The constant <see cref="TrailingMarginCompensation"/> reserves 4 px of
    /// horizontal room for the last child's negative right margin (TabItem.Margin
    /// = "0,0,-4,0") which would otherwise render past the panel's right edge.
    /// </para>
    /// </summary>
    public class TabWrapPanel : Panel
    {
        // Matches TabItem.Margin right value (-4) in the DarkTheme styles.
        // Last tab in each row overshoots by this amount; we reserve room for it.
        private const double TrailingMarginCompensation = 5;

        // Padding compensation for the slanted top-right corner (CornerRadius
        // 2,12,0,0). Geometrically-centred labels look offset LEFT to the eye
        // because the corner cut removes visual mass from the upper-right.
        // We rebalance Padding (keeping total horizontal padding constant so
        // DesiredSize.Width doesn't change → no layout loop) by shifting up
        // to MaxPaddingBias pixels of right padding to the left side, scaled
        // by how much the tab has been stretched beyond its natural width.
        private const double BasePaddingHorizontal = 15;
        private const double MaxPaddingBias = 12;
        private const double FullBiasStretch = 20; // px of stretch at which to apply full bias

        private static void ApplyPaddingBias( Control child, double arrangedWidth )
        {
            if ( child is not TabItem tab )
                return;

            double natural = tab.DesiredSize.Width;
            double stretch = Math.Max( 0, arrangedWidth - natural );
            double ratio = Math.Min( 1.0, stretch / FullBiasStretch );
            double bias = MaxPaddingBias * ratio;

            var desired = new Thickness(
                BasePaddingHorizontal + bias,
                tab.Padding.Top,
                BasePaddingHorizontal - bias,
                tab.Padding.Bottom );

            if ( tab.Padding != desired )
                tab.Padding = desired;
        }

        protected override Size MeasureOverride( Size availableSize )
        {
            int n = Children.Count;
            if ( n == 0 )
                return new Size( 0, 0 );

            double maxChildHeight = 0;
            double sumWidth = 0;
            for ( int i = 0; i < n; i++ )
            {
                Children[i].Measure( new Size( double.PositiveInfinity, availableSize.Height ) );
                Size s = Children[i].DesiredSize;
                sumWidth += s.Width;
                if ( s.Height > maxChildHeight )
                    maxChildHeight = s.Height;
            }

            double avail = availableSize.Width;

            // Single row fits — natural width.
            if ( double.IsPositiveInfinity( avail ) || sumWidth <= avail )
                return new Size( sumWidth, maxChildHeight );

            int rows = ComputeRowCount( avail );
            return new Size( avail, maxChildHeight * rows );
        }

        protected override Size ArrangeOverride( Size finalSize )
        {
            int n = Children.Count;
            if ( n == 0 )
                return finalSize;

            double maxChildHeight = 0;
            double sumWidth = 0;
            for ( int i = 0; i < n; i++ )
            {
                Size s = Children[i].DesiredSize;
                sumWidth += s.Width;
                if ( s.Height > maxChildHeight )
                    maxChildHeight = s.Height;
            }

            // Single row at natural widths.
            if ( sumWidth <= finalSize.Width )
            {
                double x = 0;
                for ( int i = 0; i < n; i++ )
                {
                    double w = Children[i].DesiredSize.Width;
                    ApplyPaddingBias( Children[i], w );
                    Children[i].Arrange( new Rect( x, 0, w, maxChildHeight ) );
                    x += w;
                }
                return new Size( finalSize.Width, maxChildHeight );
            }

            // Wrapped — proportionally stretched per row, with the row containing
            // the selected child moved to the bottom.
            int rows = ComputeRowCount( finalSize.Width );
            int childrenPerRow = (int) Math.Ceiling( (double) n / rows );

            int selectedRow = FindSelectedRow( childrenPerRow, rows );
            int bottomRow = rows - 1;

            // Row remap: logical row index -> visual row index.
            // If selected is not in the bottom row, swap selected row with bottom row.
            for ( int r = 0; r < rows; r++ )
            {
                int visualRow = r;
                if ( selectedRow >= 0 && selectedRow != bottomRow )
                {
                    if ( r == selectedRow ) visualRow = bottomRow;
                    else if ( r == bottomRow ) visualRow = selectedRow;
                }

                int start = r * childrenPerRow;
                int end = Math.Min( start + childrenPerRow, n );
                int countInRow = end - start;
                if ( countInRow == 0 )
                    break;

                // Sum natural widths in this row, then distribute the row's
                // available width (minus the trailing-margin reserve) in proportion.
                double rowNaturalSum = 0;
                for ( int j = start; j < end; j++ )
                    rowNaturalSum += Children[j].DesiredSize.Width;
                if ( rowNaturalSum <= 0 )
                    rowNaturalSum = 1;

                double rowAvailable = Math.Max( 0, finalSize.Width - TrailingMarginCompensation );
                double y = visualRow * maxChildHeight;
                double prevRight = 0;
                double cumulativeNatural = 0;
                for ( int j = start; j < end; j++ )
                {
                    cumulativeNatural += Children[j].DesiredSize.Width;
                    // Use rowAvailable exactly for the last tab so accumulated
                    // floating-point rounding doesn't leave the row 1 px short.
                    double right = ( j == end - 1 )
                        ? rowAvailable
                        : cumulativeNatural / rowNaturalSum * rowAvailable;
                    double arrangedWidth = right - prevRight;
                    ApplyPaddingBias( Children[j], arrangedWidth );
                    Children[j].Arrange( new Rect( prevRight, y, arrangedWidth, maxChildHeight ) );
                    prevRight = right;
                }
            }

            return new Size( finalSize.Width, maxChildHeight * rows );
        }

        private int FindSelectedRow( int childrenPerRow, int rows )
        {
            for ( int i = 0; i < Children.Count; i++ )
            {
                if ( Children[i] is TabItem tvi && tvi.IsSelected )
                {
                    int row = i / childrenPerRow;
                    return Math.Min( row, rows - 1 );
                }
            }
            return -1;
        }

        private int ComputeRowCount( double availableWidth )
        {
            int n = Children.Count;
            for ( int rows = 2; rows <= n; rows++ )
            {
                int childrenPerRow = (int) Math.Ceiling( (double) n / rows );
                double maxRowSum = 0;
                for ( int r = 0; r < rows; r++ )
                {
                    int start = r * childrenPerRow;
                    int end = Math.Min( start + childrenPerRow, n );
                    double rowSum = 0;
                    for ( int j = start; j < end; j++ )
                        rowSum += Children[j].DesiredSize.Width;
                    if ( rowSum > maxRowSum )
                        maxRowSum = rowSum;
                }
                if ( maxRowSum <= availableWidth )
                    return rows;
            }
            return n;
        }
    }
}
