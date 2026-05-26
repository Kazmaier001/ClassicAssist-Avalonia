using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AnimatedGif;
using Assistant;
using Avalonia.Threading;
using ClassicAssist.Shared.UI;
using ClassicAssist.UI.Views;

namespace ClassicAssist.UI.ViewModels
{
    public class GIFRecorderViewModel : BaseViewModel
    {
        private readonly GIFRecorderWindow _window;
        private bool _isRecording;
        private MemoryStream _lastStream;
        private ICommand _recordCommand;
        private ICommand _saveCommand;
        private CancellationTokenSource _token;

        public GIFRecorderViewModel( GIFRecorderWindow window )
        {
            _window = window;
        }

        public GIFRecorderViewModel()
        {
        }

        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                SetProperty( ref _isRecording, value );
                ( SaveCommand as RelayCommand )?.RaiseCanExecuteChanged();
            }
        }

        public MemoryStream LastStream
        {
            get => _lastStream;
            set
            {
                SetProperty( ref _lastStream, value );
                ( SaveCommand as RelayCommand )?.RaiseCanExecuteChanged();
            }
        }

        // GIF recording is Windows-only. The Linux path previously routed frames through
        // LinuxScreenCapture.CaptureRect (X11 XGetImage), which is dead under GNOME Wayland
        // (rootless XWayland) and too slow on real X11 to keep up with 5 FPS regardless.
        // The "right" Linux path is xdg-desktop-portal ScreenCast + PipeWire, which is a
        // multi-hundred-LOC undertaking we've chosen not to invest in. The recorder window
        // can still be opened on Linux so menu wiring stays consistent, but the Record
        // button is greyed out.
        public ICommand RecordCommand => _recordCommand ?? ( _recordCommand = new RelayCommand( RecordIfSupported, o => _window != null && OperatingSystem.IsWindows() ) );

        private void RecordIfSupported( object obj )
        {
            if ( !OperatingSystem.IsWindows() )
            {
                return;
            }
            Record( obj );
        }

        public ICommand SaveCommand => _saveCommand ?? ( _saveCommand = new RelayCommand( Save, o => !IsRecording && LastStream != null ) );

        private void Save( object obj )
        {
            if ( LastStream == null )
            {
                return;
            }

            string directory = Path.Combine( Engine.StartupPath ?? Environment.CurrentDirectory, "Screenshots" );

            if ( !Directory.Exists( directory ) )
            {
                Directory.CreateDirectory( directory );
            }

            DateTime now = DateTime.Now;

            string fileName =
                $"ClassicAssist-{now.Year}-{now.Month}-{now.Day}-{now.Hour}-{now.Minute}-{now.Second}.gif";

            string fullPath = Path.Combine( directory, fileName );

            using ( FileStream fs = new FileStream( fullPath, FileMode.Create ) )
            {
                LastStream.WriteTo( fs );
            }

            if ( OperatingSystem.IsWindows() )
            {
                string args = $"/e, /select, \"{fullPath}\"";
                ProcessStartInfo info = new ProcessStartInfo { FileName = "explorer", Arguments = args };
                Process.Start( info );
            }
        }

        [SupportedOSPlatform( "windows" )]
        private void Record( object obj )
        {
            if ( IsRecording )
            {
                _token?.Cancel();
                return;
            }

            IsRecording = true;
            _token = new CancellationTokenSource();

            Task.Run( async () =>
            {
                MemoryStream ms = new MemoryStream();
                try
                {
                    TimeSpan frameInterval = TimeSpan.FromSeconds( 1.0 / 5 );

                    using ( AnimatedGifCreator gif = new AnimatedGifCreator( ms, frameInterval.Milliseconds ) )
                    {
                        ( int x, int y, int w, int h ) = await Dispatcher.UIThread.InvokeAsync( GetCaptureRect );

                        if ( w <= 0 || h <= 0 )
                        {
                            return;
                        }

                        Bitmap bmp = new Bitmap( w, h );

                        Stopwatch sw = new Stopwatch();

                        while ( !_token.IsCancellationRequested )
                        {
                            sw.Restart();

                            ( x, y, _, _ ) = await Dispatcher.UIThread.InvokeAsync( GetCaptureRect );

                            using ( Graphics g = Graphics.FromImage( bmp ) )
                            {
                                g.CopyFromScreen( new Point( x, y ), Point.Empty, new Size( w, h ) );
                            }

                            await gif.AddFrameAsync( bmp, -1, GifQuality.Bit8 );

                            int wait = frameInterval.Milliseconds - (int) sw.ElapsedMilliseconds;

                            if ( wait > 0 )
                            {
                                try { await Task.Delay( wait, _token.Token ); }
                                catch ( TaskCanceledException ) { break; }
                            }
                        }

                        bmp.Dispose();
                    }
                }
                finally
                {
                    await Dispatcher.UIThread.InvokeAsync( () =>
                    {
                        if ( LastStream != null )
                        {
                            LastStream.Dispose();
                            LastStream = null;
                        }

                        LastStream = ms;
                        IsRecording = false;
                    } );
                }
            } );
        }

        // Capture interior of the window, matching the WPF source's +5/+50 offset and -10/-70 size
        // (skip 5px borders, 50px title bar, 20px bottom). Returns screen-pixel coordinates.
        private (int x, int y, int w, int h) GetCaptureRect()
        {
            double s = _window.RenderScaling;
            var pos = _window.Position;
            var size = _window.ClientSize;

            int pxW = (int) ( size.Width * s );
            int pxH = (int) ( size.Height * s );
            int dx = (int) ( 5 * s );
            int dyTop = (int) ( 50 * s );
            int dyBottom = (int) ( 20 * s );

            return ( pos.X + dx, pos.Y + dyTop, pxW - 2 * dx, pxH - dyTop - dyBottom );
        }
    }
}
