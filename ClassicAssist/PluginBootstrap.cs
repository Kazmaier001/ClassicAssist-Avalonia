using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ClassicAssist
{
    // Runs ONCE when the ClassicAssist assembly's module is first touched (e.g. when CUO's
    // bootstrap calls Assembly.LoadFile on our plugin DLL and then reflects out Engine.Install).
    // Hooking AssemblyResolve here is essential because CUO's host process probes its OWN
    // folder for dependencies, not our plugin folder. Our plugin pulls in Avalonia, Newtonsoft,
    // etc. — none of which live next to ClassicUO.exe. Without this hook, JIT'ing Engine.Install
    // throws TypeLoadException ("could not load Avalonia.Base") which CUO swallows silently,
    // and our log line never gets a chance to run.
    internal static class PluginBootstrap
    {
        private static bool s_installed;
        private static string s_pluginDir;

        [ModuleInitializer]
        internal static void Initialize()
        {
            if ( s_installed ) return;
            s_installed = true;

            try
            {
                s_pluginDir = Path.GetDirectoryName( typeof( PluginBootstrap ).Assembly.Location );
                if ( string.IsNullOrEmpty( s_pluginDir ) ) return;

                AppDomain.CurrentDomain.AssemblyResolve += OnResolve;

                LogEarly( $"PluginBootstrap installed. PluginDir='{s_pluginDir}'" );
            }
            catch ( Exception ex )
            {
                LogEarly( $"PluginBootstrap.Initialize threw: {ex}" );
            }
        }

        private static Assembly OnResolve( object sender, ResolveEventArgs args )
        {
            try
            {
                var name = new AssemblyName( args.Name ).Name;
                if ( string.IsNullOrEmpty( name ) ) return null;

                // 1) plugin folder root
                string candidate = Path.Combine( s_pluginDir, name + ".dll" );
                if ( File.Exists( candidate ) )
                {
                    var asm = Assembly.LoadFrom( candidate );
                    LogEarly( $"AssemblyResolve {args.Name} -> {candidate}" );
                    return asm;
                }

                // 2) culture sub-folder (resources)
                var culture = new AssemblyName( args.Name ).CultureName;
                if ( !string.IsNullOrEmpty( culture ) )
                {
                    string culturePath = Path.Combine( s_pluginDir, culture, name + ".dll" );
                    if ( File.Exists( culturePath ) )
                    {
                        var asm = Assembly.LoadFrom( culturePath );
                        LogEarly( $"AssemblyResolve {args.Name} -> {culturePath}" );
                        return asm;
                    }
                }

                LogEarly( $"AssemblyResolve {args.Name} -> NOT FOUND under '{s_pluginDir}'" );
            }
            catch ( Exception ex )
            {
                LogEarly( $"AssemblyResolve {args.Name} threw: {ex.Message}" );
            }
            return null;
        }

        // Independent of Engine's logger so we can trace even if Engine itself fails to load.
        internal static void LogEarly( string msg )
        {
            string line = $"[{DateTime.Now:O}] {msg}\n";
            try { File.AppendAllText( Path.Combine( Path.GetTempPath(), "ClassicAssist.bootstrap.log" ), line ); } catch { }
            try
            {
                if ( !string.IsNullOrEmpty( s_pluginDir ) )
                    File.AppendAllText( Path.Combine( s_pluginDir, "ClassicAssist.bootstrap.log" ), line );
            }
            catch { }
        }
    }
}
