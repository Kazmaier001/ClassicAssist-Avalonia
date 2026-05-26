using Avalonia.Controls;
using System.Threading.Tasks;

namespace ClassicAssist.Misc
{
    public static class WindowExtensions
    {
        /// <summary>
        /// ShowDialog compatibility shim. In Avalonia, ShowDialog requires a parent window.
        /// This finds the best parent window to use.
        /// </summary>
        public static Task ShowDialogCompat( this Window window )
        {
            if ( Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop )
            {
                var parent = desktop.MainWindow;
                if ( parent != null && parent != window )
                {
                    return window.ShowDialog( parent );
                }
            }
            // Fallback: show as regular window
            window.Show();
            return Task.CompletedTask;
        }

        public static Task<TResult> ShowDialogCompat<TResult>( this Window window )
        {
            if ( Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop )
            {
                var parent = desktop.MainWindow;
                if ( parent != null && parent != window )
                {
                    return window.ShowDialog<TResult>( parent );
                }
            }
            window.Show();
            return Task.FromResult( default(TResult) );
        }
    }
}