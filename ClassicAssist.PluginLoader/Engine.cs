using System;
using System.IO;
using System.Runtime.InteropServices;
using CUO_API;

namespace Assistant
{
    // CUO's ClassicUO.Bootstrap (net472) reflection-invokes Assistant.Engine.Install on a
    // PluginHeader*. This loader matches that signature so the host's Assembly.LoadFile +
    // GetType + GetMethod + Invoke chain reaches us. Once inside Install we use
    // hostfxr/nethost to bring up a CoreCLR side-by-side in the same process, then load the
    // real net10 ClassicAssist plugin and forward the PluginHeader pointer through a function
    // pointer obtained from coreclr_delegates.h's load_assembly_and_get_function_pointer.
    //
    // After that the managed plugin writes its own Marshal.GetFunctionPointerForDelegate
    // pointers into PluginHeader for OnRecv/OnSend/etc, and CUO calls those directly without
    // crossing back through this loader. Net48 here is purely a Install-time bootstrap.
    public static class Engine
    {
        // Matches the [UnmanagedCallersOnly] entry point on the net10 side.
        [UnmanagedFunctionPointer( CallingConvention.Cdecl )]
        private delegate void UnmanagedInstallDelegate( IntPtr pluginPtr );

        public static unsafe void Install( PluginHeader* plugin )
        {
            string logPath = LogPath();
            try { File.WriteAllText( logPath, "" ); } catch { }
            Log( "PluginLoader.Install entered (net48 host, will start CoreCLR side-by-side)." );

            try
            {
                string pluginDir = Path.GetDirectoryName( typeof( Engine ).Assembly.Location );
                Log( "PluginDir = '" + pluginDir + "'" );

                if ( string.IsNullOrEmpty( pluginDir ) )
                {
                    Log( "FATAL: PluginDir is empty." );
                    return;
                }

                string runtimeConfig = Path.Combine( pluginDir, "ClassicAssist.runtimeconfig.json" );
                string pluginDll = Path.Combine( pluginDir, "ClassicAssist.dll" );

                if ( !File.Exists( runtimeConfig ) )
                {
                    Log( "FATAL: missing runtimeconfig.json at " + runtimeConfig );
                    return;
                }
                if ( !File.Exists( pluginDll ) )
                {
                    Log( "FATAL: missing plugin DLL at " + pluginDll );
                    return;
                }

                var loadFn = RuntimeHost.Initialize( pluginDir, runtimeConfig, Log );
                Log( "CoreCLR ready. Resolving Assistant.Engine.UnmanagedInstall in net10 plugin..." );

                IntPtr installFnPtr;
                int rc = loadFn(
                    pluginDll,
                    "Assistant.Engine, ClassicAssist",
                    "UnmanagedInstall",
                    RuntimeHost.UNMANAGEDCALLERSONLY_METHOD,
                    IntPtr.Zero,
                    out installFnPtr );
                if ( rc != 0 || installFnPtr == IntPtr.Zero )
                {
                    Log( $"FATAL: load_assembly_and_get_function_pointer rc=0x{rc:X8} fnptr=0x{installFnPtr.ToInt64():X16}" );
                    return;
                }

                Log( $"Forwarding to managed Install... fnptr=0x{installFnPtr.ToInt64():X16} plugin=0x{((IntPtr) plugin).ToInt64():X16}" );
                var del = (UnmanagedInstallDelegate) Marshal.GetDelegateForFunctionPointer( installFnPtr, typeof( UnmanagedInstallDelegate ) );
                del( (IntPtr) plugin );
                Log( "Managed Install returned." );
            }
            catch ( Exception ex )
            {
                Log( "FATAL in PluginLoader.Install: " + ex );
                var inner = ex.InnerException;
                while ( inner != null )
                {
                    Log( "  Inner: " + inner );
                    inner = inner.InnerException;
                }
            }
        }

        private static string LogPath()
        {
            try
            {
                string dir = Path.GetDirectoryName( typeof( Engine ).Assembly.Location );
                if ( !string.IsNullOrEmpty( dir ) )
                    return Path.Combine( dir, "loader.log" );
            }
            catch { }
            return Path.Combine( Path.GetTempPath(), "ClassicAssist.loader.log" );
        }

        private static void Log( string msg )
        {
            string line = "[" + DateTime.Now.ToString( "o" ) + "] " + msg + "\n";
            try { File.AppendAllText( LogPath(), line ); } catch { }
            // Mirror to %TEMP% as a fallback if the plugin-dir path becomes unreachable.
            try { File.AppendAllText( Path.Combine( Path.GetTempPath(), "ClassicAssist.loader.log" ), line ); } catch { }
        }
    }
}
