using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace ClassicAssist.Misc
{
    /// <summary>
    /// Extension method providing a parameterless ShowDialog() shim for Avalonia windows.
    /// In WPF, ShowDialog() needs no parent; Avalonia requires one.
    /// </summary>
    public static class WindowDialogExtensions
    {
        /// <summary>
        ///     Fire-and-forget shim: opens the window modally and returns
        ///     immediately. Callers that need to read state set by the dialog
        ///     before it closes (e.g. <c>Result</c>) MUST use
        ///     <see cref="ShowDialogAsync" /> instead — this overload does
        ///     NOT block.
        /// </summary>
        public static void ShowDialog( this Window window )
        {
            Window parent = GetVisibleParent( window );

            if ( parent != null )
            {
                window.ShowDialog( parent ).ConfigureAwait( false );
            }
            else
            {
                window.Show();
            }
        }

        /// <summary>
        ///     Awaitable parameterless ShowDialog. Completes when the window
        ///     closes, mirroring the blocking semantics of WPF's
        ///     <c>Window.ShowDialog()</c>. Use this when the caller needs to
        ///     inspect dialog state (e.g. SelectedItem / Result) after the
        ///     user dismisses the window.
        /// </summary>
        public static Task ShowDialogAsync( this Window window )
        {
            Window parent = GetVisibleParent( window );

            if ( parent != null )
            {
                return window.ShowDialog( parent );
            }

            // No visible parent available — fall back to Show + wait-for-Closed.
            var tcs = new TaskCompletionSource<object>();
            window.Closed += ( _, _ ) => tcs.TrySetResult( null );
            window.Show();
            return tcs.Task;
        }

        // Avalonia's ShowDialog throws "Cannot show window with non-visible owner"
        // when the candidate parent is hidden (e.g. minimized-to-tray). Treat hidden
        // the same as no-parent so callers fall back to Show().
        private static Window GetVisibleParent( Window self )
        {
            if ( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
            {
                Window main = desktop.MainWindow;
                if ( main != null && main != self && main.IsVisible )
                {
                    return main;
                }
            }
            return null;
        }
    }
}
