using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SkiaSharp;

namespace ClassicAssist.Misc
{
    // Minimal libX11 wrapper for screen capture on Linux. Used by
    // ScreenshotTabViewModel.TakeScreenshot and GIFRecorderViewModel as the Linux
    // counterpart to NativeMethods.cs's GDI BitBlt / PrintWindow capture.
    //
    // X11 XGetImage works under both pure X11 sessions and XWayland — which is what
    // SDL3 falls back to under GNOME Wayland when its native Wayland backend is unavailable
    // or disabled. Pure-Wayland sessions where CUO runs as a native Wayland client have no
    // analogous "capture another window's pixels" primitive; xdg-desktop-portal screenshot
    // would be required there and is left for a future pass.
    [SupportedOSPlatform( "linux" )]
    internal static class LinuxScreenCapture
    {
        // Capture a rectangle of the X11 root window. Coordinates are in screen pixels.
        // Returns null if X cannot be opened (e.g. no DISPLAY) or capture fails.
        internal static SKBitmap CaptureRect( int x, int y, int width, int height )
        {
            if ( width <= 0 || height <= 0 )
            {
                return null;
            }

            IntPtr display = OpenDisplay();
            if ( display == IntPtr.Zero )
            {
                return null;
            }

            try
            {
                IntPtr root = XDefaultRootWindow( display );
                IntPtr image = XGetImage( display, root, x, y, (uint) width, (uint) height,
                    AllPlanes, ZPixmap );
                if ( image == IntPtr.Zero )
                {
                    string sessionType = Environment.GetEnvironmentVariable( "XDG_SESSION_TYPE" );
                    if ( string.Equals( sessionType, "wayland", StringComparison.OrdinalIgnoreCase ) )
                    {
                        Console.Error.WriteLine(
                            "[LinuxScreenCapture] XGetImage on root window failed under Wayland. " +
                            "GNOME/KDE Wayland compositors block X11 root capture (XWayland runs " +
                            "rootless). Workarounds: (a) launch ClassicUO with SDL_VIDEODRIVER=x11 " +
                            "to give it a real X11 window we can grab, or (b) install grim/gnome-screenshot " +
                            "and let us shell out (not yet wired)." );
                    }
                    else
                    {
                        Console.Error.WriteLine( $"[LinuxScreenCapture] XGetImage({x},{y},{width},{height}) returned NULL" );
                    }
                    return null;
                }

                try
                {
                    return CopyImageToSkBitmap( image, width, height );
                }
                finally
                {
                    XDestroyImage( image );
                }
            }
            finally
            {
                XCloseDisplay( display );
            }
        }

        // Captures the whole virtual screen (sum of all monitors via the X11 root window).
        internal static SKBitmap CaptureScreen()
        {
            IntPtr display = OpenDisplay();
            if ( display == IntPtr.Zero )
            {
                return null;
            }

            int width, height;
            try
            {
                IntPtr root = XDefaultRootWindow( display );
                if ( !XGetWindowAttributes( display, root, out XWindowAttributes attrs ) )
                {
                    Console.Error.WriteLine( "[LinuxScreenCapture] XGetWindowAttributes(root) failed" );
                    return null;
                }
                width = attrs.width;
                height = attrs.height;
            }
            finally
            {
                XCloseDisplay( display );
            }

            return CaptureRect( 0, 0, width, height );
        }

        // Wrap XOpenDisplay with diagnostics — Mono can launch CUO with DISPLAY/XAUTHORITY in
        // the environment, but those env vars only matter if the DllImport's dlopen also
        // resolves to a real libX11. Try NULL first (lets X11 read DISPLAY env), then fall
        // back to "$DISPLAY" passed explicitly so we can tell the two failure modes apart.
        private static IntPtr OpenDisplay()
        {
            IntPtr d;
            try
            {
                d = XOpenDisplay( IntPtr.Zero );
            }
            catch ( DllNotFoundException ex )
            {
                Console.Error.WriteLine( "[LinuxScreenCapture] libX11.so.6 not found: " + ex.Message );
                return IntPtr.Zero;
            }
            catch ( Exception ex )
            {
                Console.Error.WriteLine( "[LinuxScreenCapture] XOpenDisplay(NULL) threw: " + ex );
                return IntPtr.Zero;
            }

            if ( d != IntPtr.Zero )
            {
                return d;
            }

            string display = Environment.GetEnvironmentVariable( "DISPLAY" );
            string xauth = Environment.GetEnvironmentVariable( "XAUTHORITY" );
            Console.Error.WriteLine( $"[LinuxScreenCapture] XOpenDisplay(NULL) returned NULL. DISPLAY='{display}' XAUTHORITY='{xauth}'" );

            if ( string.IsNullOrEmpty( display ) )
            {
                return IntPtr.Zero;
            }

            // Retry with explicit name in case the libX11 in this process couldn't read env.
            IntPtr displayUtf8 = Marshal.StringToCoTaskMemUTF8( display );
            try
            {
                d = XOpenDisplay( displayUtf8 );
                if ( d == IntPtr.Zero )
                {
                    Console.Error.WriteLine( $"[LinuxScreenCapture] XOpenDisplay('{display}') also returned NULL." );
                }
                return d;
            }
            finally
            {
                Marshal.FreeCoTaskMem( displayUtf8 );
            }
        }

        // XImage memory layout for a 24/32-bit truecolor screen on x86 is BGRA
        // (low byte is blue). SKColorType.Bgra8888 matches byte-for-byte; we copy row by
        // row to honour the X server's bytes_per_line which may be padded > 4*width.
        private static unsafe SKBitmap CopyImageToSkBitmap( IntPtr image, int width, int height )
        {
            XImage* img = (XImage*) image;

            SKBitmap bitmap = new SKBitmap( new SKImageInfo( width, height,
                SKColorType.Bgra8888, SKAlphaType.Opaque ) );

            IntPtr dstPtr = bitmap.GetPixels();
            int dstStride = width * 4;
            int srcStride = img->bytes_per_line;
            int copyBytes = Math.Min( srcStride, dstStride );

            byte* dst = (byte*) dstPtr;
            byte* src = (byte*) img->data;

            for ( int row = 0; row < height; row++ )
            {
                Buffer.MemoryCopy( src + row * srcStride, dst + row * dstStride, dstStride, copyBytes );
            }

            // X11 alpha bits are unused in 24-bit visuals; force opaque so the PNG
            // encoder doesn't bake the random byte into a translucent pixel.
            for ( int i = 3; i < width * height * 4; i += 4 )
            {
                dst[i] = 0xFF;
            }

            return bitmap;
        }

        // Locate the CUO/SDL top-level window in the X server's window tree and return
        // its absolute screen-pixel bounds. Used by "UO Only" screenshot mode on Linux
        // to crop the fullscreen capture down to the CUO window. Returns null if no SDL
        // window is found in the X tree — which is the normal case on rootless XWayland
        // when CUO is a native Wayland client (callers should then fall back to
        // fullscreen). Works on real X11 sessions and on XWayland when CUO is launched
        // with SDL_VIDEODRIVER=x11.
        internal static (int X, int Y, int Width, int Height)? TryFindSdlWindowBounds()
        {
            IntPtr display = OpenDisplay();
            if ( display == IntPtr.Zero )
            {
                return null;
            }

            try
            {
                IntPtr root = XDefaultRootWindow( display );
                IntPtr sdlWindow = FindSdlWindow( display, root );
                if ( sdlWindow == IntPtr.Zero )
                {
                    return null;
                }

                if ( !XGetWindowAttributes( display, sdlWindow, out XWindowAttributes attrs ) )
                {
                    return null;
                }

                // attrs.x/y are relative to the window's parent (often a WM frame), not
                // root. Translate (0,0) of the window to root coordinates to get absolute
                // screen position.
                if ( XTranslateCoordinates( display, sdlWindow, root, 0, 0,
                         out int rootX, out int rootY, out _ ) == 0 )
                {
                    return null;
                }

                return ( rootX, rootY, attrs.width, attrs.height );
            }
            finally
            {
                XCloseDisplay( display );
            }
        }

        private static IntPtr FindSdlWindow( IntPtr display, IntPtr root )
        {
            // BFS the X window tree looking for a window whose WM_CLASS res_class
            // starts with "SDL" AND belongs to our own process (so we don't
            // accidentally grab Steam or another SDL app's window). Limit
            // depth/breadth so a pathological tree can't hang us.
            const int maxVisited = 512;
            int ourPid = System.Diagnostics.Process.GetCurrentProcess().Id;
            IntPtr netWmPidAtom = XInternAtom( display, "_NET_WM_PID", false );

            System.Collections.Generic.Queue<IntPtr> queue =
                new System.Collections.Generic.Queue<IntPtr>();
            queue.Enqueue( root );
            int visited = 0;

            while ( queue.Count > 0 && visited < maxVisited )
            {
                IntPtr w = queue.Dequeue();
                visited++;

                if ( w != root
                     && WindowClassStartsWith( display, w, "SDL" )
                     && GetWindowPid( display, w, netWmPidAtom ) == ourPid )
                {
                    return w;
                }

                if ( XQueryTree( display, w, out _, out _, out IntPtr children, out uint nchildren ) == 0
                     || children == IntPtr.Zero )
                {
                    continue;
                }

                try
                {
                    int count = (int) Math.Min( nchildren, 64u );
                    for ( int i = 0; i < count; i++ )
                    {
                        IntPtr child = Marshal.ReadIntPtr( children, i * IntPtr.Size );
                        queue.Enqueue( child );
                    }
                }
                finally
                {
                    XFree( children );
                }
            }

            return IntPtr.Zero;
        }

        private static int GetWindowPid( IntPtr display, IntPtr window, IntPtr netWmPidAtom )
        {
            if ( netWmPidAtom == IntPtr.Zero )
            {
                return 0;
            }

            // XA_CARDINAL = 6.
            int status = XGetWindowProperty( display, window, netWmPidAtom, IntPtr.Zero,
                new IntPtr( 1 ), false, new IntPtr( 6 ),
                out IntPtr _actualType, out int _actualFormat,
                out IntPtr nItems, out IntPtr _bytesAfter, out IntPtr prop );

            if ( status != 0 || prop == IntPtr.Zero || nItems == IntPtr.Zero )
            {
                if ( prop != IntPtr.Zero ) XFree( prop );
                return 0;
            }

            try
            {
                // _NET_WM_PID is a CARDINAL (32-bit) but Xlib hands it back as a long-sized
                // slot on 64-bit systems. Read as IntPtr to be safe.
                int pid = (int) (long) Marshal.ReadIntPtr( prop );
                return pid;
            }
            finally
            {
                XFree( prop );
            }
        }

        private static bool WindowClassStartsWith( IntPtr display, IntPtr window, string prefix )
        {
            if ( XGetClassHint( display, window, out XClassHint hint ) == 0 )
            {
                return false;
            }

            try
            {
                string resClass = hint.res_class != IntPtr.Zero
                    ? Marshal.PtrToStringAnsi( hint.res_class )
                    : null;
                return !string.IsNullOrEmpty( resClass )
                       && resClass.StartsWith( prefix, StringComparison.OrdinalIgnoreCase );
            }
            finally
            {
                if ( hint.res_name != IntPtr.Zero ) XFree( hint.res_name );
                if ( hint.res_class != IntPtr.Zero ) XFree( hint.res_class );
            }
        }

        // ---------------- X11 P/Invokes ----------------

        private const long AllPlanes = ~0L;
        private const int ZPixmap = 2;

        [DllImport( "libX11.so.6" )]
        private static extern IntPtr XOpenDisplay( IntPtr displayName );

        [DllImport( "libX11.so.6" )]
        private static extern int XCloseDisplay( IntPtr display );

        [DllImport( "libX11.so.6" )]
        private static extern IntPtr XDefaultRootWindow( IntPtr display );

        [DllImport( "libX11.so.6" )]
        private static extern IntPtr XGetImage( IntPtr display, IntPtr drawable, int x, int y,
            uint width, uint height, long plane_mask, int format );

        [DllImport( "libX11.so.6" )]
        private static extern int XDestroyImage( IntPtr image );

        [DllImport( "libX11.so.6" )]
        private static extern bool XGetWindowAttributes( IntPtr display, IntPtr window,
            out XWindowAttributes attributes );

        [DllImport( "libX11.so.6" )]
        private static extern int XQueryTree( IntPtr display, IntPtr window,
            out IntPtr root_return, out IntPtr parent_return,
            out IntPtr children_return, out uint nchildren_return );

        [DllImport( "libX11.so.6" )]
        private static extern int XFree( IntPtr data );

        [DllImport( "libX11.so.6" )]
        private static extern int XGetClassHint( IntPtr display, IntPtr window,
            out XClassHint class_hints_return );

        [DllImport( "libX11.so.6" )]
        private static extern int XTranslateCoordinates( IntPtr display,
            IntPtr src_w, IntPtr dest_w, int src_x, int src_y,
            out int dest_x_return, out int dest_y_return, out IntPtr child_return );

        [DllImport( "libX11.so.6" )]
        private static extern IntPtr XInternAtom( IntPtr display, string atom_name, bool only_if_exists );

        [DllImport( "libX11.so.6" )]
        private static extern int XGetWindowProperty( IntPtr display, IntPtr window,
            IntPtr property, IntPtr long_offset, IntPtr long_length, bool delete,
            IntPtr req_type,
            out IntPtr actual_type_return, out int actual_format_return,
            out IntPtr nitems_return, out IntPtr bytes_after_return,
            out IntPtr prop_return );

        [StructLayout( LayoutKind.Sequential )]
        private struct XClassHint
        {
            public IntPtr res_name;
            public IntPtr res_class;
        }

        // X11 XImage struct — only the fields we read.
        [StructLayout( LayoutKind.Sequential )]
        private struct XImage
        {
            public int width;
            public int height;
            public int xoffset;
            public int format;
            public IntPtr data;
            public int byte_order;
            public int bitmap_unit;
            public int bitmap_bit_order;
            public int bitmap_pad;
            public int depth;
            public int bytes_per_line;
            public int bits_per_pixel;
            // function pointer table follows; we don't touch it.
        }

        [StructLayout( LayoutKind.Sequential )]
        private struct XWindowAttributes
        {
            public int x, y;
            public int width, height;
            public int border_width;
            public int depth;
            public IntPtr visual;
            public IntPtr root;
            public int @class;
            public int bit_gravity;
            public int win_gravity;
            public int backing_store;
            public ulong backing_planes;
            public ulong backing_pixel;
            public bool save_under;
            public IntPtr colormap;
            public bool map_installed;
            public int map_state;
            public long all_event_masks;
            public long your_event_mask;
            public long do_not_propagate_mask;
            public bool override_redirect;
            public IntPtr screen;
        }
    }
}
