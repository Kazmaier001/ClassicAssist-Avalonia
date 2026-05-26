using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Avalonia.VisualTree;
using System.Linq;

namespace ClassicAssist.UI.Misc
{
    /*
     * https://weblogs.asp.net/akjoshi/Attached-behavior-for-auto-scrolling-containers-while-doing-drag-amp-drop
     */

    public class ScrollOnDragDrop : Behavior<ScrollViewer>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.AddHandler( DragDrop.DragOverEvent, OnContainerPreviewDragOver );
        }

        protected override void OnDetaching()
        {
            AssociatedObject.RemoveHandler( DragDrop.DragOverEvent, OnContainerPreviewDragOver );

            base.OnDetaching();
        }

        private static void OnContainerPreviewDragOver( object sender, DragEventArgs e )
        {
            if ( !( sender is Control container ) )
            {
                return;
            }

            ScrollViewer scrollViewer = container as ScrollViewer ?? GetFirstVisualChild<ScrollViewer>( container );

            if ( scrollViewer == null )
            {
                return;
            }

            const double tolerance = 60;
            double verticalPos = e.GetPosition( container ).Y;
            const double offset = 20;

            if ( verticalPos < tolerance ) // Top of visible list? 
            {
                scrollViewer.Offset = scrollViewer.Offset.WithY( scrollViewer.Offset.Y - offset ); //Scroll up.
            }
            else if ( verticalPos > container.Bounds.Height - tolerance ) //Bottom of visible list? 
            {
                scrollViewer.Offset = scrollViewer.Offset.WithY( scrollViewer.Offset.Y + offset ); //Scroll down.
            }
        }

        public static T GetFirstVisualChild<T>( Visual depObj ) where T : Visual
        {
            if ( depObj == null )
            {
                return null;
            }

            foreach ( var child in depObj.GetVisualChildren() )
            {
                if ( child is T result )
                {
                    return result;
                }

                T childItem = GetFirstVisualChild<T>( child );

                if ( childItem != null )
                {
                    return childItem;
                }
            }

            return null;
        }
    }
}