using Avalonia.Threading;
using ClassicAssist.UI.Views;

namespace ClassicAssist.Data.Hotkeys.Commands
{
    [HotkeyCommand( Category = "Commands", Name = "Show GIF Capture" )]
    public class ShowGIFCaptureWindow : HotkeyCommand
    {
        private GIFRecorderWindow _window;

        public override void Execute()
        {
            // The WPF version spawned an STA thread and called ShowDialog modally;
            // Avalonia has a single UI thread, so post the show onto it. GIF capture
            // itself still requires Windows (System.Drawing); the window just lets
            // the user start/stop recording.
            Dispatcher.UIThread.Post( () =>
            {
                if ( _window != null )
                {
                    _window.Activate();
                    return;
                }

                _window = new GIFRecorderWindow();
                _window.Closed += ( s, e ) => _window = null;
                _window.Show();
            } );
        }
    }
}