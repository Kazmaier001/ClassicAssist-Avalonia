#region License

// Copyright (C) 2023 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;
using Assistant;
using ClassicAssist.Data;
using ClassicAssist.Data.Macros.Commands;
using ClassicAssist.Data.Regions;
using ClassicAssist.Data.Screenshot;
using ClassicAssist.Data.Targeting;
using ClassicAssist.Misc;
using ClassicAssist.Shared.Misc;
using ClassicAssist.Shared.UI;
using ClassicAssist.UI.ViewModels.Agents.Screenshot;
using ClassicAssist.UI.Views.Agents.Screenshot;
using ClassicAssist.UI.Views.OptionsTab;
using ClassicAssist.UO.Objects;
using Microsoft.Scripting.Utils;
using Newtonsoft.Json.Linq;
using static ClassicAssist.Misc.NativeMethods;
using FlowDirection = Avalonia.Media.FlowDirection;

namespace ClassicAssist.UI.ViewModels.Agents
{
    public class ScreenshotTabViewModel : BaseViewModel, ISettingProvider
    {
        private const string SCREENSHOT_DIRECTORY_NAME = "Screenshots";

        private const string DEFAULT_FILENAME_FORMAT = "ClassicAssist-{date}-{longTime}";

        private static ICommand _takeSnapshotCommand;
        private readonly ScreenshotComparer _comparer = new ScreenshotComparer();
        private readonly string[] _extensions = { ".png", ".gif" };
        private bool _autoScreenshot;
        private Color _backgroundColor;
        private ICommand _configureFilterCommand;
        private int _distance;
        private string _filenameFormat;
        private Color _fontColor;
        private int _fontSize;
        private string _format;
        private bool _fullscreen;
        private bool _includeInfoBar;
        private bool _mobileDeath;
        private int _mobileDeathDelay;
        private List<ScreenshotMobileFilterEntry> _mobileDeathFilter = new List<ScreenshotMobileFilterEntry>();
        private bool _onlyIfEnemy;
        private ICommand _openFolderCommand;
        private RelayCommand _openScreenshotCommand;
        private bool _playerDeath;
        private int _playerDeathDelay;
        private string _screenshotPath;
        private ObservableCollection<ScreenshotEntry> _screenshots = new ObservableCollection<ScreenshotEntry>();
        private ICommand _setBackgroundColourCommand;
        private ICommand _setFontColourCommand;
        private FileSystemWatcher _watcher;

        public ScreenshotTabViewModel()
        {
            ScreenshotManager manager = ScreenshotManager.GetInstance();
            manager.TakeScreenshot = TakeScreenshot;
            manager.OnPlayerDeath = OnPlayerDeath;
            manager.OnMobileDeath = OnMobileDeath;
        }

        public bool AutoScreenshot
        {
            get => _autoScreenshot;
            set => SetProperty( ref _autoScreenshot, value );
        }

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set => SetProperty( ref _backgroundColor, value );
        }

        public ICommand ConfigureFilterCommand =>
            _configureFilterCommand ?? ( _configureFilterCommand = new RelayCommand( ConfigureFilter ) );

        public int Distance
        {
            get => _distance;
            set => SetProperty( ref _distance, value );
        }

        public string FilenameFormat
        {
            get => _filenameFormat;
            set => SetProperty( ref _filenameFormat, value );
        }

        public Color FontColor
        {
            get => _fontColor;
            set => SetProperty( ref _fontColor, value );
        }

        public int FontSize
        {
            get => _fontSize;
            set => SetProperty( ref _fontSize, value );
        }

        public string Format
        {
            get => _format;
            set => SetProperty( ref _format, value );
        }

        public bool Fullscreen
        {
            get => _fullscreen;
            set
            {
                if ( _fullscreen == value )
                {
                    return;
                }

                SetProperty( ref _fullscreen, value );
                OnPropertyChanged( nameof( UoOnly ) );
            }
        }

        // Paired with Fullscreen so the two checkboxes are mutually exclusive.
        // Original WPF used RadioButtons which auto-grouped within a panel; the
        // Avalonia port switched them to CheckBoxes (Fluent RadioButton padding
        // looked off) and lost the auto-grouping. Routing both bindings through
        // a single underlying property + paired notify-change is more reliable
        // than the previous inverse-converter round-trip, which intermittently
        // left both checkboxes ticked because Avalonia's IsChecked is bool?.
        public bool UoOnly
        {
            get => !_fullscreen;
            set
            {
                if ( _fullscreen == !value )
                {
                    return;
                }

                Fullscreen = !value;
            }
        }

        public bool IncludeInfoBar
        {
            get => _includeInfoBar;
            set
            {
                SetProperty( ref _includeInfoBar, value );
                ( _setBackgroundColourCommand as RelayCommand )?.RaiseCanExecuteChanged();
                ( _setFontColourCommand as RelayCommand )?.RaiseCanExecuteChanged();
            }
        }

        public bool MobileDeath
        {
            get => _mobileDeath;
            set => SetProperty( ref _mobileDeath, value );
        }

        public int MobileDeathDelay
        {
            get => _mobileDeathDelay;
            set => SetProperty( ref _mobileDeathDelay, value );
        }

        public List<ScreenshotMobileFilterEntry> MobileDeathFilter
        {
            get => _mobileDeathFilter;
            set => SetProperty( ref _mobileDeathFilter, value );
        }

        public bool OnlyIfEnemy
        {
            get => _onlyIfEnemy;
            set => SetProperty( ref _onlyIfEnemy, value );
        }

        public ICommand OpenFolderCommand =>
            _openFolderCommand ?? ( _openFolderCommand = new RelayCommand( OpenFolder ) );

        public ICommand OpenScreenshotCommand =>
            _openScreenshotCommand ?? ( _openScreenshotCommand = new RelayCommand( OpenScreenshot, o => o != null ) );

        public bool PlayerDeath
        {
            get => _playerDeath;
            set => SetProperty( ref _playerDeath, value );
        }

        public int PlayerDeathDelay
        {
            get => _playerDeathDelay;
            set => SetProperty( ref _playerDeathDelay, value );
        }

        public ObservableCollection<ScreenshotEntry> Screenshots
        {
            get => _screenshots;
            set => SetProperty( ref _screenshots, value );
        }

        public ICommand SetBackgroundColourCommand =>
            _setBackgroundColourCommand ?? ( _setBackgroundColourCommand =
                new RelayCommand( SetBackgroundColour, o => IncludeInfoBar ) );

        public ICommand SetFontColourCommand =>
            _setFontColourCommand ?? ( _setFontColourCommand = new RelayCommand( SetFontColour, o => IncludeInfoBar ) );

        public ICommand TakeSnapshotCommand =>
            _takeSnapshotCommand ?? ( _takeSnapshotCommand = new RelayCommand( TakeSnapshot ) );

        public void Serialize( JObject json, bool global = false )
        {
            if ( json == null )
            {
                return;
            }

            JObject obj = new JObject
            {
                { "Fullscreen", Fullscreen },
                { "FilenameFormat", FilenameFormat },
                { "IncludeInfoBar", IncludeInfoBar },
                { "Format", Format },
                { "FontSize", FontSize },
                { "FontColor", FontColor.ToString() },
                { "BackgroundColor", BackgroundColor.ToString() },
                { "AutoScreenshot", AutoScreenshot },
                { "PlayerDeath", PlayerDeath },
                { "PlayerDeathDelay", PlayerDeathDelay },
                { "MobileDeath", MobileDeath },
                { "MobileDeathDelay", MobileDeathDelay },
                { "Distance", Distance },
                { "OnlyIfEnemy", OnlyIfEnemy }
            };

            JArray filter = new JArray();

            foreach ( ScreenshotMobileFilterEntry entry in MobileDeathFilter )
            {
                filter.Add( new JObject { { "ID", entry.ID }, { "Note", entry.Note }, { "Enabled", entry.Enabled } } );
            }

            obj.Add( "MobileDeathFilter", filter );
            json.Add( "Screenshot", obj );
        }

        public void Deserialize( JObject json, Options options, bool global = false )
        {
            if ( _watcher != null )
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }

            _screenshotPath = Path.Combine( Engine.StartupPath, SCREENSHOT_DIRECTORY_NAME );

            if ( !Directory.Exists( _screenshotPath ) )
            {
                Directory.CreateDirectory( _screenshotPath );
            }

            string[] files = _extensions.SelectMany( ext => Directory.GetFiles( _screenshotPath, $"*{ext}" ) )
                .ToArray();

            foreach ( string file in files )
            {
                AddScreenshot( file );
            }

            _watcher = new FileSystemWatcher( _screenshotPath, "*.*" ) { EnableRaisingEvents = true };
            _watcher.Created += OnScreenshotCreated;
            _watcher.Deleted += OnScreenshotDeleted;

            if ( json == null )
            {
                return;
            }

            // Workaround for profiles having no settings, if FontSize = 0 set obj to empty object
            // So the defaults get set, can remove in the future
            if ( json["Screenshot"] is null || json["Screenshot"] is JObject &&
                json["Screenshot"]["FontSize"] is JValue val && val.Value<int>() == 0 )
            {
                json["Screenshot"] = new JObject();
            }

            Fullscreen = json["Screenshot"]["Fullscreen"]?.ToObject<bool>() ?? false;
            FilenameFormat = json["Screenshot"]["FilenameFormat"]?.ToObject<string>() ?? DEFAULT_FILENAME_FORMAT;
            IncludeInfoBar = json["Screenshot"]["IncludeInfoBar"]?.ToObject<bool>() ?? true;
            Format = json["Screenshot"]["Format"]?.ToObject<string>() ?? "{player} ({shard}) - {date} {time}";
            FontSize = json["Screenshot"]["FontSize"]?.ToObject<int>() ?? 16;
            BackgroundColor = json["Screenshot"]["BackgroundColor"]?.ToObject<Color>() ?? Colors.Black;
            FontColor = json["Screenshot"]["FontColor"]?.ToObject<Color>() ?? Colors.White;
            AutoScreenshot = json["Screenshot"]["AutoScreenshot"]?.ToObject<bool>() ?? false;
            PlayerDeath = json["Screenshot"]["PlayerDeath"]?.ToObject<bool>() ?? false;
            PlayerDeathDelay = json["Screenshot"]["PlayerDeathDelay"]?.ToObject<int>() ?? 2000;
            MobileDeath = json["Screenshot"]["MobileDeath"]?.ToObject<bool>() ?? false;
            MobileDeathDelay = json["Screenshot"]["MobileDeathDelay"]?.ToObject<int>() ?? 500;
            Distance = json["Screenshot"]["Distance"]?.ToObject<int>() ?? 12;
            OnlyIfEnemy = json["Screenshot"]["OnlyIfEnemy"]?.ToObject<bool>() ?? false;

            if ( json["Screenshot"]["MobileDeathFilter"] is JArray mobileIdArray )
            {
                MobileDeathFilter = mobileIdArray.Select( e =>
                {
                    JObject obj = (JObject) e;

                    return new ScreenshotMobileFilterEntry
                    {
                        ID = obj["ID"]?.ToObject<int>() ?? 0,
                        Note = obj["Note"]?.ToObject<string>() ?? string.Empty,
                        Enabled = obj["Enabled"]?.ToObject<bool>() ?? false
                    };
                } ).ToList();
            }
            else
            {
                MobileDeathFilter = GetDefaultMobileIDs();
            }
        }

        private static List<ScreenshotMobileFilterEntry> GetDefaultMobileIDs()
        {
            TargetManager targetManager = TargetManager.GetInstance();

            return targetManager.BodyData
                .Where( b => b.BodyType == TargetBodyType.Humanoid && !b.Name.Contains( "Dead" ) ).Select( b =>
                    new ScreenshotMobileFilterEntry { ID = b.Graphic, Note = b.Name, Enabled = true } ).ToList();
        }

        private async void ConfigureFilter( object obj )
        {
            ScreenshotMobileFilterViewModel vm = new ScreenshotMobileFilterViewModel();

            vm.Items.AddRange( MobileDeathFilter );

            ScreenshotMobileFilterWindow window = new ScreenshotMobileFilterWindow { DataContext = vm };

            // ShowDialog() is fire-and-forget in Avalonia — must await so
            // vm.DialogResult is meaningful when the dialog closes.
            await window.ShowDialogAsync();

            if ( vm.DialogResult == true )
            {
                MobileDeathFilter = vm.Items.ToList();
            }
        }

        private void OnMobileDeath( Mobile mobile )
        {
            if ( !AutoScreenshot || !MobileDeath || !MobileDeathFilter.Any( e => e.ID == mobile.ID && e.Enabled ) ||
                 mobile.Distance > Distance )
            {
                return;
            }

            if ( OnlyIfEnemy )
            {
                int enemy = AliasCommands.GetAlias( "enemy" );

                if ( enemy != mobile.Serial )
                {
                    return;
                }
            }

            try
            {
                Task.Run( async () =>
                {
                    if ( MobileDeathDelay > 0 )
                    {
                        await Task.Delay( MobileDeathDelay );
                    }

                    TakeScreenshot( null, mobile.Name );
                } );
            }
            catch ( Exception )
            {
                // ignored
            }
        }

        public void OnPlayerDeath( string name )
        {
            if ( !AutoScreenshot || !PlayerDeath )
            {
                return;
            }

            try
            {
                Task.Run( async () =>
                {
                    if ( PlayerDeathDelay > 0 )
                    {
                        await Task.Delay( PlayerDeathDelay );
                    }

                    TakeScreenshot( null, name );
                } );
            }
            catch ( Exception )
            {
                // ignored
            }
        }

        private static void TakeSnapshot( object obj )
        {
            MainCommands.Snapshot();
        }

        private async void SetBackgroundColour( object obj )
        {
            if ( !( obj is Color colour ) )
            {
                return;
            }

            MacrosGumpTextColorSelectorViewModel vm =
                new MacrosGumpTextColorSelectorViewModel { SelectedColor = colour, AllowAlpha = true };

            MacrosGumpTextColorSelectorWindow window = new MacrosGumpTextColorSelectorWindow { DataContext = vm };

            // Must await — Avalonia's ShowDialog() is fire-and-forget, so a
            // synchronous vm.Result check fires before the user even sees the
            // dialog and is always false. See [[avalonia-click-vs-command-order]]
            // and Misc/WindowDialogExtensions.cs.
            await window.ShowDialogAsync();

            if ( !vm.Result )
            {
                return;
            }

            BackgroundColor = vm.SelectedColor;
        }

        private async void SetFontColour( object obj )
        {
            if ( !( obj is Color colour ) )
            {
                return;
            }

            MacrosGumpTextColorSelectorViewModel vm =
                new MacrosGumpTextColorSelectorViewModel { SelectedColor = colour, AllowAlpha = true };

            MacrosGumpTextColorSelectorWindow window = new MacrosGumpTextColorSelectorWindow { DataContext = vm };

            await window.ShowDialogAsync();

            if ( !vm.Result )
            {
                return;
            }

            FontColor = vm.SelectedColor;
        }

        private void OnScreenshotDeleted( object sender, FileSystemEventArgs e )
        {
            ScreenshotEntry screenshot = Screenshots.FirstOrDefault( s => s.Path.Equals( e.FullPath ) );

            if ( screenshot != null )
            {
                Dispatcher.UIThread.Invoke( () => { Screenshots.Remove( screenshot ); } );
            }
        }

        private void AddScreenshot( string file )
        {
            Dispatcher.UIThread.Invoke( () =>
            {
                if ( Screenshots.Any( s => s.Path.Equals( file ) ) )
                {
                    return;
                }

                Screenshots.AddSorted(
                    new ScreenshotEntry
                    {
                        Path = file,
                        Bitmap = new Lazy<Bitmap>( () => LoadBitmap( file ) ),
                        CreatedDate = File.GetCreationTime( file ),
                        Extension = Path.GetExtension( file ).Replace( ".", string.Empty ).ToUpper()
                    }, _comparer );
            } );
        }

        private static Bitmap LoadBitmap( string file )
        {
            if ( !File.Exists( file ) )
            {
                return null;
            }

            try
            {
                return new Bitmap( file );
            }
            catch
            {
                return null;
            }
        }

        private void OnScreenshotCreated( object sender, FileSystemEventArgs e )
        {
            if ( !_extensions.Contains( Path.GetExtension( e.FullPath ) ) )
            {
                return;
            }

            Task.Delay( 1000 ).ContinueWith( t => AddScreenshot( e.FullPath ) );
        }

        private static void OpenScreenshot( object obj )
        {
            if ( !( obj is ScreenshotEntry screenshot ) )
            {
                return;
            }

            try
            {
                Process.Start( new ProcessStartInfo( screenshot.Path ) { UseShellExecute = true } );
            }
            catch
            {
                // We tried
            }
        }

        private void OpenFolder( object obj )
        {
            if ( !Directory.Exists( _screenshotPath ) )
            {
                return;
            }

            try
            {
                Process.Start( new ProcessStartInfo( _screenshotPath ) { UseShellExecute = true } );
            }
            catch
            {
                // We tried
            }
        }

        private static readonly Lazy<SKBitmap> _screenshotLogo = new Lazy<SKBitmap>( LoadScreenshotLogo );

        private static SKBitmap LoadScreenshotLogo()
        {
            try
            {
                using ( Stream stream = AssetLoader.Open(
                           new Uri( "avares://ClassicAssist/Resources/screenshot_logo.png" ) ) )
                {
                    return SKBitmap.Decode( stream );
                }
            }
            catch
            {
                return null;
            }
        }

        public string TakeScreenshot( bool? fullscreen = null, string mobileName = null, string filename = null )
        {
            bool fs = fullscreen.HasValue && fullscreen.Value || Fullscreen;

            SKBitmap captured;

            if ( OperatingSystem.IsWindows() )
            {
                captured = CaptureWindows( fs );
            }
            else if ( OperatingSystem.IsLinux() )
            {
                captured = CaptureLinux( fs );
                if ( captured == null )
                {
                    Console.Error.WriteLine(
                        "[Screenshot] TakeScreenshot: all Linux capture backends failed. See stderr above." );
                    return null;
                }
            }
            else
            {
                Console.Error.WriteLine(
                    "[Screenshot] TakeScreenshot: capture path not yet ported for this OS." );
                return null;
            }

            if ( captured == null )
            {
                return null;
            }

            DateTime now = DateTime.Now;

            string filePath =
                $"{GetFormattedText( !string.IsNullOrEmpty( filename ) ? filename : FilenameFormat, now, mobileName, true )}.png";

            try
            {
                using ( SKCanvas canvas = new SKCanvas( captured ) )
                {
                    SKBitmap logo = _screenshotLogo.Value;

                    if ( logo != null )
                    {
                        using ( SKPaint logoPaint = new SKPaint
                                {
                                    IsAntialias = true,
                                    ColorFilter = SKColorFilter.CreateColorMatrix( new[]
                                    {
                                        1f, 0f, 0f, 0f, 0f,
                                        0f, 1f, 0f, 0f, 0f,
                                        0f, 0f, 1f, 0f, 0f,
                                        0f, 0f, 0f, 0.6f, 0f
                                    } )
                                } )
                        {
                            canvas.DrawBitmap( logo, captured.Width - logo.Width - 5, 5, logoPaint );
                        }
                    }

                    if ( IncludeInfoBar )
                    {
                        string text = GetFormattedText( Format, now, mobileName );

                        using ( SKTypeface typeface = SKTypeface.FromFamilyName( "Arial" ) )
                        using ( SKFont font = new SKFont( typeface, FontSize ) )
                        using ( SKPaint bgPaint = new SKPaint
                                {
                                    Color = new SKColor( BackgroundColor.R, BackgroundColor.G,
                                        BackgroundColor.B, BackgroundColor.A ),
                                    IsAntialias = true,
                                    Style = SKPaintStyle.Fill
                                } )
                        using ( SKPaint fgPaint = new SKPaint
                                {
                                    Color = new SKColor( FontColor.R, FontColor.G, FontColor.B, FontColor.A ),
                                    IsAntialias = true
                                } )
                        {
                            float textWidth = font.MeasureText( text );
                            SKFontMetrics metrics = font.Metrics;
                            float textHeight = metrics.Descent - metrics.Ascent;

                            float boxW = textWidth + 10;
                            float boxH = textHeight + 10;

                            canvas.DrawRoundRect( new SKRect( 0, 0, boxW, boxH ), 5, 5, bgPaint );
                            canvas.DrawText( text, 5, 5 - metrics.Ascent, SKTextAlign.Left, font, fgPaint );
                        }
                    }

                    canvas.Flush();
                }

                if ( !Path.IsPathRooted( filePath ) )
                {
                    string path = Path.Combine( Engine.StartupPath, SCREENSHOT_DIRECTORY_NAME );

                    if ( !Directory.Exists( path ) )
                    {
                        Directory.CreateDirectory( path );
                    }

                    filePath = Path.Combine( path, filePath );
                }

                using ( SKImage image = SKImage.FromBitmap( captured ) )
                using ( SKData data = image.Encode( SKEncodedImageFormat.Png, 100 ) )
                using ( FileStream stream = File.OpenWrite( filePath ) )
                {
                    data.SaveTo( stream );
                }
            }
            finally
            {
                captured.Dispose();
            }

            return filePath;
        }

        [System.Runtime.Versioning.SupportedOSPlatform( "linux" )]
        private static SKBitmap CaptureLinux( bool fullscreen )
        {
            if ( fullscreen )
            {
                // X11 XGetImage on the root works on real X11 sessions. Falls back to
                // gnome-screenshot / grim shell-out on rootless XWayland (GNOME Wayland).
                return LinuxScreenCapture.CaptureScreen() ?? LinuxScreenshotHelper.TryCapture();
            }

            // UO Only — try paths in order from cheapest/best to fallback:
            //   1. X11 finder + crop. Works on real X11 sessions or
            //      SDL_VIDEODRIVER=x11. Cropped pixels are perfect.
            //   2. gnome-screenshot -w (active-window mode). Captures whatever window
            //      is focused — on Wayland clients can't focus-steal, so the caller
            //      (or in-game hotkey) must have CUO focused.
            //   3. Fullscreen as a graceful last resort with a diagnostic.
            var bounds = LinuxScreenCapture.TryFindSdlWindowBounds();
            if ( bounds is { } b )
            {
                SKBitmap full = LinuxScreenCapture.CaptureScreen() ?? LinuxScreenshotHelper.TryCapture();
                if ( full == null ) return null;
                SKBitmap cropped = CropSkBitmap( full, b.X, b.Y, b.Width, b.Height );
                if ( cropped == null )
                {
                    return full; // bounds were off-screen; better to return something
                }
                full.Dispose();
                return cropped;
            }

            SKBitmap windowShot = LinuxScreenshotHelper.TryCapture( activeWindowOnly: true );
            if ( windowShot != null )
            {
                return windowShot;
            }

            Console.Error.WriteLine(
                "[Screenshot] UO Only: no usable window-capture backend (no SDL X11 window, " +
                "no gnome-screenshot). Falling back to fullscreen." );
            return LinuxScreenCapture.CaptureScreen() ?? LinuxScreenshotHelper.TryCapture();
        }

        // Clamp the requested rect to the source bitmap's bounds and blit a copy. Returns
        // null if the rect ends up empty (entirely outside the source).
        private static SKBitmap CropSkBitmap( SKBitmap src, int x, int y, int w, int h )
        {
            int x0 = Math.Max( 0, x );
            int y0 = Math.Max( 0, y );
            int x1 = Math.Min( src.Width, x + w );
            int y1 = Math.Min( src.Height, y + h );
            int cw = x1 - x0;
            int ch = y1 - y0;
            if ( cw <= 0 || ch <= 0 )
            {
                return null;
            }

            SKBitmap dst = new SKBitmap( new SKImageInfo( cw, ch, src.ColorType, src.AlphaType ) );
            using ( SKCanvas canvas = new SKCanvas( dst ) )
            {
                canvas.DrawBitmap( src, new SKRect( x0, y0, x1, y1 ), new SKRect( 0, 0, cw, ch ) );
            }
            return dst;
        }

        [System.Runtime.Versioning.SupportedOSPlatform( "windows" )]
        private static SKBitmap CaptureWindows( bool fullscreen )
        {
            // GDI BitBlt against a per-window DC (`GetDC(hWnd)`) returns a black
            // bitmap when the window renders via Vulkan/D3D/OpenGL. Strategy:
            //   - Fullscreen: BitBlt from desktop DC (DWM-composed, sees GPU output).
            //   - Per-window: PrintWindow with PW_RENDERFULLCONTENT — DWM renders
            //     the window's own contents into our DC, EXCLUDING any window
            //     that happens to be on top (e.g. the Avalonia MainWindow).
            IntPtr screenDC = GetDC( IntPtr.Zero );

            if ( screenDC == IntPtr.Zero )
            {
                return null;
            }

            int width, height;
            IntPtr hWnd = IntPtr.Zero;

            if ( fullscreen )
            {
                width = GetSystemMetrics( SM_CXVIRTUALSCREEN );
                height = GetSystemMetrics( SM_CYVIRTUALSCREEN );
            }
            else
            {
                hWnd = Engine.WindowHandle;
                GetClientRect( hWnd, out RECT rect );
                width = rect.Right - rect.Left;
                height = rect.Bottom - rect.Top;
            }

            if ( width <= 0 || height <= 0 )
            {
                ReleaseDC( IntPtr.Zero, screenDC );
                return null;
            }

            IntPtr memDC = CreateCompatibleDC( screenDC );
            IntPtr hBitmap = CreateCompatibleBitmap( screenDC, width, height );
            IntPtr previousObject = SelectObject( memDC, hBitmap );

            if ( fullscreen )
            {
                int srcX = GetSystemMetrics( SM_XVIRTUALSCREEN );
                int srcY = GetSystemMetrics( SM_YVIRTUALSCREEN );
                BitBlt( memDC, 0, 0, width, height, screenDC, srcX, srcY, TernaryRasterOperations.SRCCOPY );
            }
            else
            {
                // PrintWindow draws the WHOLE window (NC + client) into the DC at (0,0).
                // After the call, the client area is offset by the window border/title
                // bar size — but CUO is typically borderless fullscreen-windowed, so
                // those insets are zero and the resulting bitmap matches client coords.
                PrintWindow( hWnd, memDC, PW_RENDERFULLCONTENT );
            }

            SKImageInfo info = new SKImageInfo( width, height, SKColorType.Bgra8888, SKAlphaType.Opaque );
            SKBitmap bitmap = new SKBitmap( info );

            BITMAPINFOHEADER bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint) System.Runtime.InteropServices.Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height, // negative = top-down DIB so rows match SKBitmap layout
                biPlanes = 1,
                biBitCount = 32,
                biCompression = BI_RGB
            };

            int rows = GetDIBits( memDC, hBitmap, 0, (uint) height, bitmap.GetPixels(), ref bmiHeader,
                DIB_RGB_COLORS );

            SelectObject( memDC, previousObject );
            DeleteObject( hBitmap );
            DeleteDC( memDC );
            ReleaseDC( IntPtr.Zero, screenDC );

            if ( rows == 0 )
            {
                bitmap.Dispose();
                return null;
            }

            return bitmap;
        }

        private static string GetFormattedText( string format, DateTime now, string mobileName,
            bool filenameChars = false )
        {
            if ( filenameChars && string.IsNullOrEmpty( format ) )
            {
                format = DEFAULT_FILENAME_FORMAT;
            }

            Dictionary<string, Func<string>> replacements = new Dictionary<string, Func<string>>
            {
                { "player", () => Engine.Player?.Name },
                { "shard", () => Engine.CurrentShard?.Name },
                { "mobile", () => mobileName },
                { "date", now.ToShortDateString },
                { "time", now.ToShortTimeString },
                { "longDate", now.ToLongDateString },
                { "longTime", now.ToLongTimeString },
                { "isoDate", () => now.ToString( "O" ) },
                { "x", () => Engine.Player?.X.ToString() },
                { "y", () => Engine.Player?.Y.ToString() },
                { "map", () => Engine.Player?.Map.ToString() },
                { "region", () => Regions.GetRegion( Engine.Player )?.Name },
                { "ticks", now.Ticks.ToString }
            };

            return Regex.Replace( format, "{(.*?)}", match =>
            {
                string key = match.Groups[1].Value;
                string replacementValue =
                    replacements.TryGetValue( key, out Func<string> replacement ) ? replacement() : key;
                string str = string.IsNullOrEmpty( replacementValue ) ? string.Empty : replacementValue;

                if ( filenameChars )
                {
                    str = Path.GetInvalidFileNameChars().Aggregate( str, ( current, c ) => current.Replace( c, '-' ) );
                }

                return str;
            } ).Trim();
        }

        public class ScreenshotComparer : IComparer<ScreenshotEntry>
        {
            public int Compare( ScreenshotEntry x, ScreenshotEntry y )
            {
                if ( ReferenceEquals( x, y ) )
                {
                    return 0;
                }

                if ( ReferenceEquals( null, y ) )
                {
                    return 1;
                }

                if ( ReferenceEquals( null, x ) )
                {
                    return -1;
                }

                return y.CreatedDate.CompareTo( x.CreatedDate );
            }
        }

        public class ScreenshotEntry
        {
            public Lazy<Bitmap> Bitmap { get; set; }
            public DateTime CreatedDate { get; set; }
            public string Extension { get; set; }
            public string Path { get; set; }
        }
    }
}