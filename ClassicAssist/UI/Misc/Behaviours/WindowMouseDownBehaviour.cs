using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.UI.Misc.Behaviours
{
    public class WindowMouseDownBehaviour : Behavior<Control>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PointerPressed += OnPointerPressed;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PointerPressed -= OnPointerPressed;
            base.OnDetaching();
        }

        private void OnPointerPressed( object sender, PointerPressedEventArgs e )
        {
            if ( !e.GetCurrentPoint( null ).Properties.IsLeftButtonPressed )
            {
                return;
            }

            if ( TopLevel.GetTopLevel( AssociatedObject ) is Window window )
            {
                window.BeginMoveDrag( e );
            }
        }
    }
}