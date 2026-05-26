using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace ClassicAssist.Misc
{
    /// <summary>
    /// Cross-platform clipboard shim replacing System.Windows.Clipboard
    /// </summary>
    public static class ClipboardCompat
    {
        private static IClipboard GetClipboard()
        {
            if ( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
            {
                return desktop.MainWindow?.Clipboard;
            }
            return null;
        }

        public static void SetText( string text )
        {
            GetClipboard()?.SetTextAsync( text ).ConfigureAwait( false );
        }

        public static string GetText()
        {
            return GetClipboard()?.GetTextAsync().GetAwaiter().GetResult() ?? string.Empty;
        }
    }
}