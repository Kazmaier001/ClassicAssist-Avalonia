using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Assistant
{
    // Wraps nethost + hostfxr to start CoreCLR side-by-side in this (net48 under Mono on Linux,
    // .NET Framework on Windows) process and return the universal "load assembly and get function
    // pointer" delegate. Reference: https://learn.microsoft.com/dotnet/core/tutorials/netcore-hosting
    //
    // Per-platform string marshalling: hostfxr's char_t is wchar_t on Windows (UTF-16) and char
    // on Unix (UTF-8). We define separate delegate sets and dispatch on the platform.
    internal static class RuntimeHost
    {
        // hostfxr_get_runtime_delegate type codes from hostfxr.h
        private const int hdt_load_assembly_and_get_function_pointer = 5;

        // Sentinel meaning "the target managed method is [UnmanagedCallersOnly]";
        // see coreclr_delegates.h: #define UNMANAGEDCALLERSONLY_METHOD ((const char_t*)-1)
        internal static readonly IntPtr UNMANAGEDCALLERSONLY_METHOD = new IntPtr( -1 );

        // ---------------- Windows (UTF-16) signatures ----------------

        [UnmanagedFunctionPointer( CallingConvention.StdCall, CharSet = CharSet.Unicode )]
        private delegate int hostfxr_initialize_for_runtime_config_w(
            [MarshalAs( UnmanagedType.LPWStr )] string runtime_config_path,
            IntPtr parameters,
            out IntPtr host_context_handle );

        [UnmanagedFunctionPointer( CallingConvention.StdCall )]
        private delegate int hostfxr_get_runtime_delegate_fn(
            IntPtr host_context_handle,
            int type,
            out IntPtr fnPtr );

        [UnmanagedFunctionPointer( CallingConvention.StdCall, CharSet = CharSet.Unicode )]
        internal delegate int load_assembly_and_get_function_pointer_w(
            [MarshalAs( UnmanagedType.LPWStr )] string assemblyPath,
            [MarshalAs( UnmanagedType.LPWStr )] string typeName,
            [MarshalAs( UnmanagedType.LPWStr )] string methodName,
            IntPtr delegateTypeName,
            IntPtr reserved,
            out IntPtr fnPtr );

        // ---------------- Unix (UTF-8) signatures ----------------

        // On Unix hostfxr exports use the standard cdecl calling convention (the StdCall vs
        // Cdecl distinction is x86 Windows specific; on x86-64 it's identical, but keep the
        // attribute correct for cleanliness).
        [UnmanagedFunctionPointer( CallingConvention.Cdecl )]
        private delegate int hostfxr_initialize_for_runtime_config_a(
            IntPtr runtime_config_path_utf8,
            IntPtr parameters,
            out IntPtr host_context_handle );

        [UnmanagedFunctionPointer( CallingConvention.Cdecl )]
        private delegate int hostfxr_get_runtime_delegate_unix(
            IntPtr host_context_handle,
            int type,
            out IntPtr fnPtr );

        [UnmanagedFunctionPointer( CallingConvention.Cdecl )]
        internal delegate int load_assembly_and_get_function_pointer_a(
            IntPtr assemblyPath_utf8,
            IntPtr typeName_utf8,
            IntPtr methodName_utf8,
            IntPtr delegateTypeName,
            IntPtr reserved,
            out IntPtr fnPtr );

        // ---------------- Public adapter: one delegate-shaped wrapper -----------------

        // Caller-facing identical signature regardless of platform. Internally we own the
        // string marshalling (UTF-16 vs UTF-8) and forward to the correct native fnptr.
        internal delegate int load_assembly_and_get_function_pointer_fn(
            string assemblyPath,
            string typeName,
            string methodName,
            IntPtr delegateTypeName,
            IntPtr reserved,
            out IntPtr fnPtr );

        // ---------------- Win32 ----------------

        [DllImport( "kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
        private static extern IntPtr LoadLibrary( string lpFileName );

        [DllImport( "kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true )]
        private static extern IntPtr GetProcAddress( IntPtr hModule, string procName );

        [DllImport( "kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
        [return: MarshalAs( UnmanagedType.Bool )]
        private static extern bool SetDllDirectory( string lpPathName );

        // nethost exports get_hostfxr_path. We load it explicitly (via the plugin folder which
        // we add to the DLL search path) so the DllImport doesn't fail before we can log.
        [DllImport( "nethost.dll", CharSet = CharSet.Unicode )]
        private static extern int get_hostfxr_path_w( [Out] StringBuilder buffer, ref IntPtr buffer_size, IntPtr parameters );

        // ---------------- libdl (glibc) ----------------

        private const int RTLD_NOW = 2;
        private const int RTLD_GLOBAL = 0x100;

        // glibc 2.34+ moved dl* into libc itself, but libdl.so.2 remains a compatibility stub
        // that re-exports them, so this entry point works on every modern distro we'd ship to.
        [DllImport( "libdl.so.2" )]
        private static extern IntPtr dlopen( string filename, int flags );

        [DllImport( "libdl.so.2" )]
        private static extern IntPtr dlsym( IntPtr handle, string symbol );

        [DllImport( "libdl.so.2" )]
        private static extern IntPtr dlerror();

        // ---------------- Initialize ----------------

        internal static load_assembly_and_get_function_pointer_fn Initialize( string pluginDir, string runtimeConfigPath, Action<string> log )
        {
            return IsWindows()
                ? InitializeWindows( pluginDir, runtimeConfigPath, log )
                : InitializeUnix( pluginDir, runtimeConfigPath, log );
        }

        private static bool IsWindows()
        {
            // net48 has RuntimeInformation, but Mono on Linux loads us with PlatformID.Unix and
            // that's enough.
            var p = Environment.OSVersion.Platform;
            return p == PlatformID.Win32NT || p == PlatformID.Win32Windows || p == PlatformID.Win32S || p == PlatformID.WinCE;
        }

        // ----- Windows path -----

        private static load_assembly_and_get_function_pointer_fn InitializeWindows( string pluginDir, string runtimeConfigPath, Action<string> log )
        {
            if ( !SetDllDirectory( pluginDir ) )
            {
                log( "WARN: SetDllDirectory failed, err=" + Marshal.GetLastWin32Error() );
            }

            string hostfxrPath = LocateHostfxrWindows( log );
            log( "hostfxr at: " + hostfxrPath );

            IntPtr hostfxr = LoadLibrary( hostfxrPath );
            if ( hostfxr == IntPtr.Zero )
            {
                throw new InvalidOperationException( "LoadLibrary(hostfxr) failed, err=" + Marshal.GetLastWin32Error() );
            }

            var init = (hostfxr_initialize_for_runtime_config_w) Marshal.GetDelegateForFunctionPointer(
                GetProcAddress( hostfxr, "hostfxr_initialize_for_runtime_config" ),
                typeof( hostfxr_initialize_for_runtime_config_w ) );
            var getDelegate = (hostfxr_get_runtime_delegate_fn) Marshal.GetDelegateForFunctionPointer(
                GetProcAddress( hostfxr, "hostfxr_get_runtime_delegate" ),
                typeof( hostfxr_get_runtime_delegate_fn ) );

            log( "Calling hostfxr_initialize_for_runtime_config: " + runtimeConfigPath );
            IntPtr ctx;
            int rc = init( runtimeConfigPath, IntPtr.Zero, out ctx );
            if ( rc < 0 || ctx == IntPtr.Zero )
            {
                throw new InvalidOperationException( $"hostfxr_initialize_for_runtime_config rc=0x{rc:X8} ctx=0x{ctx.ToInt64():X}" );
            }
            if ( rc == 1 ) log( "  (CoreCLR was already initialized — reusing)" );
            else if ( rc == 2 ) log( "  (CoreCLR initialized, different runtime properties)" );
            else log( "  (CoreCLR initialized fresh)" );

            IntPtr fnPtr;
            rc = getDelegate( ctx, hdt_load_assembly_and_get_function_pointer, out fnPtr );
            if ( rc != 0 || fnPtr == IntPtr.Zero )
            {
                throw new InvalidOperationException( $"hostfxr_get_runtime_delegate rc=0x{rc:X8}" );
            }

            var wide = (load_assembly_and_get_function_pointer_w) Marshal.GetDelegateForFunctionPointer(
                fnPtr, typeof( load_assembly_and_get_function_pointer_w ) );

            return ( string asmPath, string typeName, string methodName, IntPtr delegateTypeName, IntPtr reserved, out IntPtr outFn ) =>
                wide( asmPath, typeName, methodName, delegateTypeName, reserved, out outFn );
        }

        private static string LocateHostfxrWindows( Action<string> log )
        {
            var sb = new StringBuilder( 1024 );
            IntPtr size = (IntPtr) sb.Capacity;
            int rc = get_hostfxr_path_w( sb, ref size, IntPtr.Zero );

            if ( rc == unchecked( (int) 0x80008098 ) ) // HostApiBufferTooSmall
            {
                sb = new StringBuilder( (int) size );
                rc = get_hostfxr_path_w( sb, ref size, IntPtr.Zero );
            }

            if ( rc != 0 )
            {
                throw new InvalidOperationException( $"get_hostfxr_path failed 0x{rc:X8} — is the .NET 10 runtime installed?" );
            }

            return sb.ToString();
        }

        // ----- Unix path -----

        private static load_assembly_and_get_function_pointer_fn InitializeUnix( string pluginDir, string runtimeConfigPath, Action<string> log )
        {
            string hostfxrPath = LocateHostfxrUnix( log );
            log( "hostfxr at: " + hostfxrPath );

            IntPtr hostfxr = dlopen( hostfxrPath, RTLD_NOW | RTLD_GLOBAL );
            if ( hostfxr == IntPtr.Zero )
            {
                throw new InvalidOperationException( "dlopen(hostfxr) failed: " + LastDlError() );
            }

            var init = (hostfxr_initialize_for_runtime_config_a) Marshal.GetDelegateForFunctionPointer(
                MustDlsym( hostfxr, "hostfxr_initialize_for_runtime_config" ),
                typeof( hostfxr_initialize_for_runtime_config_a ) );
            var getDelegate = (hostfxr_get_runtime_delegate_unix) Marshal.GetDelegateForFunctionPointer(
                MustDlsym( hostfxr, "hostfxr_get_runtime_delegate" ),
                typeof( hostfxr_get_runtime_delegate_unix ) );

            log( "Calling hostfxr_initialize_for_runtime_config: " + runtimeConfigPath );
            IntPtr ctx;
            IntPtr cfgUtf8 = Utf8Alloc( runtimeConfigPath );
            int rc;
            try { rc = init( cfgUtf8, IntPtr.Zero, out ctx ); }
            finally { Marshal.FreeHGlobal( cfgUtf8 ); }

            if ( rc < 0 || ctx == IntPtr.Zero )
            {
                throw new InvalidOperationException( $"hostfxr_initialize_for_runtime_config rc=0x{rc:X8} ctx=0x{ctx.ToInt64():X}" );
            }
            if ( rc == 1 ) log( "  (CoreCLR was already initialized — reusing)" );
            else if ( rc == 2 ) log( "  (CoreCLR initialized, different runtime properties)" );
            else log( "  (CoreCLR initialized fresh)" );

            IntPtr fnPtr;
            rc = getDelegate( ctx, hdt_load_assembly_and_get_function_pointer, out fnPtr );
            if ( rc != 0 || fnPtr == IntPtr.Zero )
            {
                throw new InvalidOperationException( $"hostfxr_get_runtime_delegate rc=0x{rc:X8}" );
            }

            var ansi = (load_assembly_and_get_function_pointer_a) Marshal.GetDelegateForFunctionPointer(
                fnPtr, typeof( load_assembly_and_get_function_pointer_a ) );

            return ( string asmPath, string typeName, string methodName, IntPtr delegateTypeName, IntPtr reserved, out IntPtr outFn ) =>
            {
                IntPtr a = Utf8Alloc( asmPath );
                IntPtr t = Utf8Alloc( typeName );
                IntPtr m = Utf8Alloc( methodName );
                try { return ansi( a, t, m, delegateTypeName, reserved, out outFn ); }
                finally
                {
                    Marshal.FreeHGlobal( a );
                    Marshal.FreeHGlobal( t );
                    Marshal.FreeHGlobal( m );
                }
            };
        }

        private static string LocateHostfxrUnix( Action<string> log )
        {
            // Honour DOTNET_ROOT if set, otherwise probe known install roots. We pick the highest
            // version directory under host/fxr/. Matches what nethost.so does internally.
            string[] roots;
            string envRoot = Environment.GetEnvironmentVariable( "DOTNET_ROOT" );
            if ( !string.IsNullOrEmpty( envRoot ) )
            {
                roots = new[] { envRoot };
            }
            else
            {
                roots = new[]
                {
                    "/usr/share/dotnet",     // .NET install via Microsoft repo
                    "/usr/lib/dotnet",       // Ubuntu/Debian package
                    "/usr/local/share/dotnet",
                    "/opt/dotnet",
                    Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".dotnet" )
                };
            }

            foreach ( string root in roots )
            {
                string fxrDir = Path.Combine( root, "host", "fxr" );
                if ( !Directory.Exists( fxrDir ) )
                {
                    continue;
                }

                // Pick the highest-versioned subdirectory.
                var versions = Directory.GetDirectories( fxrDir )
                    .Select( d => new { Path = d, Name = Path.GetFileName( d ) } )
                    .Where( x => TryParseVersion( x.Name, out _ ) )
                    .OrderByDescending( x => { TryParseVersion( x.Name, out Version v ); return v; } )
                    .ToArray();

                foreach ( var v in versions )
                {
                    string candidate = Path.Combine( v.Path, "libhostfxr.so" );
                    if ( File.Exists( candidate ) )
                    {
                        log( $"  Selected hostfxr from {root} version {v.Name}" );
                        return candidate;
                    }
                }
            }

            throw new InvalidOperationException( "libhostfxr.so not found in any known .NET install root (set DOTNET_ROOT to override). Is .NET 10 runtime installed?" );
        }

        private static bool TryParseVersion( string s, out Version v )
        {
            v = null;
            try { v = new Version( s ); return true; } catch { return false; }
        }

        private static IntPtr MustDlsym( IntPtr handle, string symbol )
        {
            // Clear any leftover error so dlerror() reflects only this call.
            dlerror();
            IntPtr p = dlsym( handle, symbol );
            if ( p == IntPtr.Zero )
            {
                throw new InvalidOperationException( $"dlsym({symbol}) failed: " + LastDlError() );
            }
            return p;
        }

        private static string LastDlError()
        {
            IntPtr err = dlerror();
            return err == IntPtr.Zero ? "(no error)" : Marshal.PtrToStringAnsi( err );
        }

        private static IntPtr Utf8Alloc( string s )
        {
            if ( s == null )
            {
                return IntPtr.Zero;
            }

            byte[] bytes = Encoding.UTF8.GetBytes( s );
            IntPtr p = Marshal.AllocHGlobal( bytes.Length + 1 );
            Marshal.Copy( bytes, 0, p, bytes.Length );
            Marshal.WriteByte( p, bytes.Length, 0 );
            return p;
        }
    }
}
