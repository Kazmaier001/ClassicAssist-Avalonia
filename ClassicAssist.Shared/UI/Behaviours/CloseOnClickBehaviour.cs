using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace ClassicAssist.Shared.UI.Behaviours
{
    public class CloseOnClickBehaviour : Behavior<Button>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Click += OnClick;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Click -= OnClick;
        }

        private void OnClick( object sender, RoutedEventArgs e )
        {
            if ( !( sender is Button button ) )
            {
                return;
            }

            // Avalonia fires Button.Click BEFORE Command.Execute (opposite of WPF).
            // Closing immediately tears the window down before the bound OK/Save/etc
            // command can run — dialogs that rely on the command to set DialogResult
            // or persist state silently lose the user's input (e.g. PropertySelectionWindow
            // returned DialogResult=false even when user clicked OK). Defer the close
            // onto the dispatcher queue so the click chain — including the auto-invoked
            // Command — completes first.
            var window = button.FindAncestorOfType<Window>();
            if ( window != null )
            {
                Dispatcher.UIThread.Post( () => window.Close(), DispatcherPriority.Background );
            }
        }
    }
}