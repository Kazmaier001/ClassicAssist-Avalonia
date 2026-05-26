using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using SkiaSharp;

namespace ClassicAssist.Misc
{
    // Linux screen capture path that works on GNOME / Mutter Wayland sessions where the
    // X11 XGetImage path is dead (XWayland runs -rootless so the root has no real pixmap).
    // Tried in order:
    //   1. gnome-screenshot --file (GNOME-specific, silent CLI; apt install gnome-screenshot).
    //   2. grim (wlroots compositors — Sway/Hyprland; cheap to try, just won't work on GNOME).
    //
    // xdg-desktop-portal Screenshot was attempted but is impractical for us: the portal
    // grants permissions based on the calling process's app id, and we live inside CUO's
    // process which has no registered .desktop file. The Response signal never fires for
    // unidentified callers — silent denial that blocked our UI thread on the wait.
    [SupportedOSPlatform( "linux" )]
    internal static class LinuxScreenshotHelper
    {
        // Single shared lock so concurrent screenshot triggers (hotkey spam) don't race.
        private static readonly object _captureLock = new object();

        internal static SKBitmap TryCapture( bool activeWindowOnly = false )
        {
            string tmpPath = Path.Combine( Path.GetTempPath(),
                $"classicassist-shot-{Guid.NewGuid():N}.png" );

            try
            {
                lock ( _captureLock )
                {
                    string method = null;
                    if ( TryGnomeScreenshot( tmpPath, activeWindowOnly ) )
                    {
                        method = activeWindowOnly ? "gnome-screenshot -w" : "gnome-screenshot";
                    }
                    else if ( !activeWindowOnly && TryGrim( tmpPath ) )
                    {
                        // grim is fullscreen-only here; no active-window mode on wlroots.
                        method = "grim";
                    }

                    if ( method == null )
                    {
                        Console.Error.WriteLine(
                            "[LinuxScreenshotHelper] no working capture backend. " +
                            "Install one: `sudo apt install gnome-screenshot` (GNOME/Mutter) " +
                            "or `sudo apt install grim` (Sway/Hyprland)." );
                        return null;
                    }

                    if ( !File.Exists( tmpPath ) )
                    {
                        Console.Error.WriteLine( $"[LinuxScreenshotHelper] {method} reported success but {tmpPath} does not exist." );
                        return null;
                    }

                    Console.Error.WriteLine( $"[LinuxScreenshotHelper] captured via {method}: {new FileInfo( tmpPath ).Length} bytes" );
                    return SKBitmap.Decode( tmpPath );
                }
            }
            finally
            {
                try { File.Delete( tmpPath ); } catch { }
            }
        }

        // --- gnome-screenshot --file [--window] ---

        private static bool TryGnomeScreenshot( string outPath, bool activeWindowOnly )
        {
            if ( !ToolExists( "gnome-screenshot" ) )
            {
                return false;
            }

            // gnome-screenshot -w captures the currently focused top-level window. On
            // Wayland, clients cannot programmatically raise/focus themselves, so the
            // caller is responsible for ensuring CUO is the focused window when this
            // fires (a hotkey bound from in-game already satisfies that). When the user
            // triggers from the ClassicAssist UI, the assistant window is focused — we
            // accept that as a known limitation rather than fight the Wayland security
            // model.
            string[] args = activeWindowOnly
                ? new[] { "--window", "--file", outPath }
                : new[] { "--file", outPath };

            string err = RunProcess( "gnome-screenshot", args, 5000 );
            if ( !File.Exists( outPath ) )
            {
                Console.Error.WriteLine( "[LinuxScreenshotHelper] gnome-screenshot did not produce file. stderr: " + err );
                return false;
            }
            return true;
        }

        // --- grim ---

        private static bool TryGrim( string outPath )
        {
            if ( !ToolExists( "grim" ) )
            {
                return false;
            }
            RunProcess( "grim", new[] { outPath }, 5000 );
            return File.Exists( outPath );
        }

        // --- subprocess helpers ---

        private static bool ToolExists( string tool )
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "/usr/bin/which",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                psi.ArgumentList.Add( tool );
                using Process p = Process.Start( psi );
                p.WaitForExit( 1000 );
                return p.ExitCode == 0;
            }
            catch { return false; }
        }

        private static string RunProcess( string file, string[] args, int timeoutMs )
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = file,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                foreach ( string a in args ) { psi.ArgumentList.Add( a ); }
                using Process p = Process.Start( psi );
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                if ( !p.WaitForExit( timeoutMs ) )
                {
                    try { p.Kill( true ); } catch { }
                    return null;
                }
                return ( stdout ?? "" ) + ( stderr ?? "" );
            }
            catch ( Exception ex )
            {
                Console.Error.WriteLine( $"[LinuxScreenshotHelper] {file} failed to start: {ex.Message}" );
                return null;
            }
        }

    }
}
