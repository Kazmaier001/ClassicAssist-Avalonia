// ReSharper disable once RedundantUsingDirective

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using ClassicAssist.Data;
using ClassicAssist.Data.Abilities;
using ClassicAssist.Data.Commands;
using ClassicAssist.Data.Hotkeys;
using ClassicAssist.Data.Macros.Commands;
using ClassicAssist.Data.Misc;
using ClassicAssist.Data.Scavenger;
using ClassicAssist.Data.Targeting;
using ClassicAssist.Misc;
using ClassicAssist.Shared;
using ClassicAssist.Shared.Resources;
using ClassicAssist.UI.Views;
using ClassicAssist.UO;
using ClassicAssist.UO.Data;
using ClassicAssist.UO.Gumps;
using ClassicAssist.UO.Network;
using ClassicAssist.UO.Network.PacketFilter;
using ClassicAssist.UO.Network.Packets;
using ClassicAssist.UO.Objects;
using CUO_API;
using Newtonsoft.Json;
using Sentry;
using static ClassicAssist.Misc.SDLKeys;
using MacroManager = ClassicAssist.Data.Macros.MacroManager;

[assembly: InternalsVisibleTo( "ClassicAssist.Tests" )]
[assembly: InternalsVisibleTo( "ClassicAssist.UITest" )]

// ReSharper disable once CheckNamespace
namespace Assistant
{
    public static partial class Engine
    {
        public delegate void dClientClosing();

        public delegate void dConnected();

        public delegate void dDisconnected();

        public delegate void dFocusChanged( bool focus );

        public delegate void dHotkeyPressed( int key, int mod, Key keys, ModKey modKey );

        public delegate void dPlayerInitialized( PlayerMobile player );

        public delegate void dSendRecvPacket( byte[] data, int length );

        public delegate void dSlowHandler( PacketDirection direction, string handlerName, TimeSpan elapsed );

        public delegate void dUpdateWindowTitle();

        private const int MAX_DISTANCE = 32;

        private static OnConnected _onConnected;
        private static OnDisconnected _onDisconnected;
        private static OnPacketSendRecv _onReceive;
        private static OnPacketSendRecv _onSend;
        // IntPtr-variant host→plugin INJECTION API (the host exposes Recv_new / Send_new
        // fnptrs in the extended PluginHeader at offsets 200/208). Fully blittable, safe
        // across the CoreCLR/.NET Framework boundary. Replaces _sendToClient/_sendToServer
        // (which used the old ref-byte[] API and hangs the host on every injection).
        [UnmanagedFunctionPointer( CallingConvention.Cdecl )]
        [return: MarshalAs( UnmanagedType.I1 )]
        private delegate bool OnPacketSendRecvNewIntPtr( IntPtr data, ref int length );
        private static OnPacketSendRecvNewIntPtr _sendToClientNew;
        private static OnPacketSendRecvNewIntPtr _sendToServerNew;
        // New (post-2024 ClassicUO) plugin API: byte[] passed by value, avoids the cross-CLR
        // `ref byte[]` marshalling stub that breaks our CoreCLR-embed model. See
        // OnPacketSendNew/OnPacketReceiveNew below for context.
        //
        // The host's delegate (decompiled from ClassicUO.exe) is:
        //   [UnmanagedFunctionPointer(Cdecl)] [return: MarshalAs(I1)]
        //   delegate bool OnPacketSendRecv_new(byte[] data, ref int length);
        //
        // We MUST match the calling convention. We also MUST add SizeParamIndex on the byte[]
        // because CoreCLR's marshaller, with no size hint, defaults to creating a 1-byte array
        // — exactly what install.log showed (`data.Len=1 len=62`). With SizeParamIndex=1 the
        // marshaller allocates `byte[*length]` and copies the host buffer into it.
        [UnmanagedFunctionPointer( CallingConvention.Cdecl )]
        [return: MarshalAs( UnmanagedType.I1 )]
        // [In, Out] on the LPArray is required for incoming-packet filters that REWRITE bytes
        // in place (e.g. LightLevelFilter overlaying packet[1] on 0x4F to force brightness).
        // Without [Out], the marshaller treats data as in-only: it copies the unmanaged buffer
        // into a fresh managed byte[] on entry and never writes our mutations back, so the host
        // continues rendering the original server values. Verified 2026-05-22: 0x4F overlays
        // were logging "server level 26 -> user level 0" but the dungeon stayed dark.
        private delegate bool OnPacketSendRecvNew(
            [In, Out, MarshalAs( UnmanagedType.LPArray, SizeParamIndex = 1 )] byte[] data,
            ref int length );
        private static OnPacketSendRecvNew _onReceiveNew;
        private static OnPacketSendRecvNew _onSendNew;
        private static OnTick _onTick;
        private static OnGetUOFilePath _getUOFilePath;
        private static OnPacketSendRecv _sendToClient;
        private static OnPacketSendRecv _sendToServer;
        private static OnGetPacketLength _getPacketLength;
        private static OnUpdatePlayerPosition _onPlayerPositionChanged;
        private static OnSetTitle _setTitle;
        private static MainWindow _window;
        private static Thread _mainThread;
        private static OnClientClose _onClientClosing;
        private static readonly PacketFilter _incomingPacketFilter = new PacketFilter();
        private static readonly PacketFilter _outgoingPacketPreFilter = new PacketFilter();
        private static readonly PacketFilter _outgoingPacketPostFilter = new PacketFilter();
        private static OnHotkey _onHotkeyPressed;
        private static RequestMove _requestMove;

        private static readonly int[] _sequenceList = new int[256];
        private static OnMouse _onMouse;

        private static readonly DateTime[] _lastMouseAction = new DateTime[(int) MouseOptions.None];
        private static readonly Dictionary<Key, DateTime> _lastKeyAction = new Dictionary<Key, DateTime>();

        // SDL modifier bitmask captured from the most recent keyboard event (press or release).
        // Read by OnMouse to look up modifier+mouse hotkeys. volatile for cross-thread visibility
        // — keyboard events arrive on CUO's UI thread, mouse events on the same thread but read
        // from a Dispatcher.UIThread.Post closure that may run later.
        private static volatile int _currentModifier;
        private static readonly object _clientSendLock = new object();
        private static DateTime _nextPacketRecvTime;

        private static readonly TimeSpan PACKET_RECV_DELAY = TimeSpan.FromMilliseconds( 5 );
        private static readonly object _serverSendLock = new object();

        private static readonly TimeSpan PACKET_SEND_DELAY = TimeSpan.FromMilliseconds( 5 );
        private static DateTime _nextPacketSendTime;
        public static int LastSpellID;
        private static Stopwatch _incomingStopwatch;
        private static Stopwatch _outgoingStopwatch;
        public static int LastSkillID;
        private static OnFocusGained _onFocusGained;
        private static OnFocusLost _onFocusLost;
        private static bool _clientHasFocus;
        public static CharacterListFlags CharacterListFlags { get; set; }

        public static Assembly ClassicAssembly { get; set; }

        // Set true at the start of OnClientClosing so MainWindow's Closing handler can
        // distinguish "host is shutting us down" from "user clicked X". WPF ClassicAssist
        // doesn't allow the assistant window to close independently of CUO; we match that
        // by cancelling user-initiated close events.
        public static bool IsShuttingDown { get; private set; }

        public static string ClientPath { get; set; }
        public static Version ClientVersion { get; set; }
        public static bool Connected { get; set; }

        public static string CUOPath { get; set; }
        public static ShardEntry CurrentShard { get; set; }
        public static Dispatcher Dispatcher { get; set; }
        public static FeatureFlags Features { get; set; }
        public static GumpCollection Gumps { get; set; } = new GumpCollection();
        public static ItemCollection Items { get; set; } = new ItemCollection( 0 );
        public static CircularBuffer<JournalEntry> Journal { get; set; } = new CircularBuffer<JournalEntry>( 1024 );

        public static int KeyboardLayoutId { get; set; }

        public static DateTime LastActionPacket { get; set; }
        public static int LastPromptID { get; set; }
        public static int LastPromptSerial { get; set; }
        public static DateTime LastSkillTime { get; set; }

        public static TargetQueue<TargetQueueObject> LastTargetQueue { get; set; } = new TargetQueue<TargetQueueObject>();

        public static MenuCollection Menus { get; set; } = new MenuCollection();
        public static MobileCollection Mobiles { get; set; } = new MobileCollection( Items );
        public static PacketWaitEntries PacketWaitEntries { get; set; }
        public static PlayerMobile Player { get; set; }
        public static QuestPointerList QuestPointers { get; set; } = new QuestPointerList();
        public static RehueList RehueList { get; set; } = new RehueList();
        public static List<ShardEntry> Shards { get; set; }

        public static Dispatcher StartupDispatcher { get; set; }
        public static string StartupPath { get; set; }
        public static bool TargetExists { get; set; }
        public static TargetFlags TargetFlags { get; set; }
        public static int TargetSerial { get; set; }
        public static TargetType TargetType { get; set; }

        public static Queue<Action> TickWorkQueue { get; set; } = new Queue<Action>();
        public static bool TooltipsEnabled { get; set; }
        public static bool WaitingForTarget { get; set; }
        internal static ConcurrentDictionary<uint, int> GumpList { get; set; } = new ConcurrentDictionary<uint, int>();

        public static event dHotkeyPressed HotkeyPressedEvent;

        public static event dUpdateWindowTitle UpdateWindowTitleEvent;

        internal static event dSendRecvPacket InternalPacketSentEvent;
        internal static event dSendRecvPacket InternalPacketReceivedEvent;

        public static event dSendRecvPacket PacketReceivedEvent;
        public static event dSendRecvPacket PacketSentEvent;
        public static event dSendRecvPacket SentPacketFilteredEvent;
        public static event dSendRecvPacket ReceivedPacketFilteredEvent;
        public static event dConnected ConnectedEvent;
        public static event dDisconnected DisconnectedEvent;
        public static event dPlayerInitialized PlayerInitializedEvent;

        // Entry point invoked by ClassicAssist.PluginLoader (net48) once it has spun up
        // CoreCLR via hostfxr. The loader calls this through a function pointer obtained
        // from coreclr_delegates.h's load_assembly_and_get_function_pointer with the
        // UNMANAGEDCALLERSONLY_METHOD sentinel, so [UnmanagedCallersOnly] is required and
        // all parameters must be blittable. Exceptions escaping an UCO method terminate the
        // process, so we wrap and swallow — InstallCore handles its own diagnostic logging.
        [System.Runtime.InteropServices.UnmanagedCallersOnly( CallConvs = new[] { typeof( System.Runtime.CompilerServices.CallConvCdecl ) } )]
        public static void UnmanagedInstall( IntPtr pluginPtr )
        {
            try
            {
                unsafe { InstallCore( (PluginHeader*) pluginPtr ); }
            }
            catch
            {
                // InstallCore writes its own log on failure; nothing to do here but prevent
                // the exception from crossing back into the calling net48 CLR.
            }
        }

        // Direct in-proc entry point for ClassicAssist.Tests, which calls Engine.Install
        // with a PluginHeader* it built on the stack. Not used by the production plugin
        // loader path (that goes through UnmanagedInstall above).
        public static unsafe void Install( PluginHeader* plugin )
        {
            InstallCore( plugin );
        }


        private static unsafe void InstallCore( PluginHeader* plugin )
        {
            // Diagnostic log: write to BOTH the plugin folder AND %TEMP% so a path-resolution
            // surprise doesn't lose us the trace. ClassicUO runs Install on its own thread and
            // silently discards exceptions; without a file log a failed Install() looks
            // identical to "plugin not loaded at all".
            string pluginDir = Path.GetDirectoryName( typeof( Engine ).Assembly.Location ) ?? Path.GetTempPath();
            string logPath = Path.Combine( pluginDir, "ClassicAssist.install.log" );
            string tempLogPath = Path.Combine( Path.GetTempPath(), "ClassicAssist.install.log" );
            try { File.WriteAllText( logPath, "" ); } catch { }
            try { File.WriteAllText( tempLogPath, "" ); } catch { }
            void Log( string msg ) => InstallLog( msg );
            Log( $"Install() entered. plugin=0x{((IntPtr)plugin).ToInt64():X16} AssemblyLocation='{typeof( Engine ).Assembly.Location}' CWD='{Environment.CurrentDirectory}'" );

            // Synchronously populate PluginHeader on CUO's calling thread. This only touches
            // blittable function-pointer plumbing — does NOT touch Dispatcher.UIThread.
            try
            {
                InitializePlugin( plugin );
                Log( "PluginHeader populated on CUO thread." );
            }
            catch ( Exception ex )
            {
                Log( $"FATAL in InitializePlugin (header): {ex}" );
                throw;
            }

            // Spawn the UI thread. EVERYTHING that might touch Dispatcher.UIThread must run
            // inside AfterSetup so Avalonia has already installed the Win32 dispatcher impl.
            // Initialize() calls AssistantOptions.Load → BaseViewModel.cctor → Dispatcher.UIThread.
            // InitializeExtensions() touches the dispatcher even more directly. Doing either
            // before BuildAvaloniaApp().Setup creates the dispatcher singleton with the default
            // ManagedDispatcherImpl, and StartCore later throws PlatformNotSupportedException
            // trying to drive MainLoop. So we defer it all into AfterSetup.
            _mainThread = new Thread( () =>
            {
                try
                {
                    Log( "UI thread started. Calling BuildAvaloniaApp().StartWithClassicDesktopLifetime..." );
                    BuildAvaloniaApp()
                        .AfterSetup( builder =>
                        {
                            Log( "Avalonia setup complete (AfterSetup callback fired)." );
                            // First touch of Dispatcher.UIThread happens here, AFTER Win32
                            // platform's dispatcher impl is registered — so the singleton picks
                            // up Win32DispatcherImpl which implements IControlledDispatcherImpl.
                            StartupDispatcher = Dispatcher.UIThread;

                            try
                            {
                                Log( "Running Initialize() + InitializePluginLate() (extensions, data files)..." );
                                Initialize();
                                InitializePluginLate();
                                Log( "Initialize + InitializePluginLate OK." );
                            }
                            catch ( Exception ex )
                            {
                                Log( $"FATAL in Initialize/InitializePluginLate: {ex}" );
                            }

                            if ( builder.Instance?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop )
                            {
                                try
                                {
                                    Log( "Constructing SplashWindow..." );
                                    SplashWindow splashWindow = new SplashWindow();
                                    splashWindow.Show();
                                    Log( "Splash shown. Yielding to dispatcher so it paints..." );

                                    // MainWindow construction is ~1s of synchronous UI work that
                                    // blocks the dispatcher loop. If we kick it off inline, the
                                    // splash's first paint never runs and the window appears as
                                    // an invisible (or under classic chrome, blank) box. Post the
                                    // heavy work at Background priority so the dispatcher gets a
                                    // chance to render the splash first.
                                    Dispatcher.UIThread.Post( () =>
                                    {
                                        try
                                        {
                                            Log( "Constructing MainWindow..." );
                                            _window = new MainWindow();
                                            desktop.MainWindow = _window;
                                            // Load the profile AFTER MainWindow ctor (so BaseViewModel.Instances
                                            // is populated and Deserialize hits real VM targets) but BEFORE
                                            // Show (so the first paint reflects loaded state, not empty VMs).
                                            // Recipe mirrors TestHost.Program.LoadProfileIfAvailable.
                                            try
                                            {
                                                AssistantOptions.OnWindowLoaded();
                                                Log( $"Profile loaded: '{AssistantOptions.LastProfile}'" );
                                            }
                                            catch ( Exception ex )
                                            {
                                                Log( $"Profile load failed (continuing with empty VMs): {ex.GetType().Name}: {ex.Message}" );
                                            }
                                            _window.Show();
                                            Log( "MainWindow shown — handing control to desktop lifetime." );
                                        }
                                        catch ( Exception ex )
                                        {
                                            Log( $"FATAL in deferred MainWindow construction: {ex}" );
                                        }
                                        finally
                                        {
                                            try { splashWindow.Close(); } catch { }
                                        }
                                    }, DispatcherPriority.Background );
                                }
                                catch ( Exception ex )
                                {
                                    Log( $"FATAL in AfterSetup window-construction: {ex}" );
                                }
                            }
                            else
                            {
                                Log( $"WARN: ApplicationLifetime is not classic desktop: {builder.Instance?.ApplicationLifetime?.GetType().FullName ?? "<null>"}" );
                            }
                        } )
                        .StartWithClassicDesktopLifetime( Array.Empty<string>() );

                    Log( "Desktop lifetime returned — UI thread exiting." );
                }
                catch ( Exception ex )
                {
                    Log( $"FATAL in UI thread: {ex}" );
                }
            } ) { IsBackground = true };

            // STA is a Windows COM-apartment concept; no-op on Linux/macOS.
            if ( OperatingSystem.IsWindows() )
            {
                _mainThread.SetApartmentState( ApartmentState.STA );
            }
            _mainThread.Start();
        }

        internal static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<ClassicAssist.App>()
                .UsePlatformDetect()
                .LogToTrace();
        }

        // PluginHeader-touching part. MUST run synchronously on CUO's calling thread so the
        // header is populated by the time Install returns — CUO reads OnRecv/OnSend/etc.
        // right after. Does NOT touch Dispatcher.UIThread, so safe to run before Avalonia setup.
        internal static unsafe void InitializePlugin( PluginHeader* plugin )
        {
            _onConnected = OnConnected;
            _onDisconnected = OnDisconnected;
            _onReceive = OnPacketReceive;
            _onSend = OnPacketSend;
            _onPlayerPositionChanged = OnPlayerPositionChanged;
            _onClientClosing = OnClientClosing;
            _onHotkeyPressed = OnHotkeyPressed;
            _onMouse = OnMouse;
            _onTick = OnTick;
            _onFocusGained = () => OnFocusChanged( true );
            _onFocusLost = () => OnFocusChanged( false );
            _onSendNew = OnPacketSendNew;
            _onReceiveNew = OnPacketReceiveNew;
            WindowHandle = plugin->HWND;

            plugin->OnConnected = Marshal.GetFunctionPointerForDelegate( _onConnected );
            plugin->OnDisconnected = Marshal.GetFunctionPointerForDelegate( _onDisconnected );
            plugin->OnRecv = Marshal.GetFunctionPointerForDelegate( _onReceive );
            plugin->OnSend = Marshal.GetFunctionPointerForDelegate( _onSend );
            plugin->OnPlayerPositionChanged = Marshal.GetFunctionPointerForDelegate( _onPlayerPositionChanged );
            plugin->OnClientClosing = Marshal.GetFunctionPointerForDelegate( _onClientClosing );
            plugin->OnHotkeyPressed = Marshal.GetFunctionPointerForDelegate( _onHotkeyPressed );
            plugin->OnMouse = Marshal.GetFunctionPointerForDelegate( _onMouse );
            plugin->Tick = Marshal.GetFunctionPointerForDelegate( _onTick );
            plugin->OnFocusGained = Marshal.GetFunctionPointerForDelegate( _onFocusGained );
            plugin->OnFocusLost = Marshal.GetFunctionPointerForDelegate( _onFocusLost );

            _getPacketLength = Marshal.GetDelegateForFunctionPointer<OnGetPacketLength>( plugin->GetPacketLength );
            _getUOFilePath = Marshal.GetDelegateForFunctionPointer<OnGetUOFilePath>( plugin->GetUOFilePath );
            _sendToClient = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>( plugin->Recv );
            _sendToServer = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>( plugin->Send );
            _requestMove = Marshal.GetDelegateForFunctionPointer<RequestMove>( plugin->RequestMove );
            _setTitle = Marshal.GetDelegateForFunctionPointer<OnSetTitle>( plugin->SetTitle );

            ClientVersion = new Version( (byte) ( plugin->ClientVersion >> 24 ), (byte) ( plugin->ClientVersion >> 16 ), (byte) ( plugin->ClientVersion >> 8 ),
                (byte) plugin->ClientVersion );

            // The host's PluginHeader is larger than cuoapi.dll's CUO_API.PluginHeader — it
            // appends OnRecv_new / OnSend_new / Recv_new / Send_new / OnDrawCmdList / SDL_Window /
            // OnWndProc / GetStaticData / GetTileData / GetCliloc after SetTitle (decompiled from
            // ClassicUO.exe's internal PluginHeader). The cuoapi.dll struct only exposes the
            // first 22 fields, so we have to poke the extended ones via raw offsets.
            //
            // Wiring OnSend_new / OnRecv_new tells the host to call our new (byte[] data,
            // ref int length) entry points; ProcessSendPacket/ProcessRecvPacket prefer the _new
            // delegates over the old ref-byte[] ones. This avoids the cross-CLR marshalling
            // bug where the .NET Framework host's `_onSend(ref data, ref length)` over our
            // CoreCLR delegate leaves the host with a foreign byte[] ref, then crashes in
            // Array.Copy because the foreign MethodTable poisons element-size validation.
            const int OFFSET_ONRECV_NEW = 8 /*ClientVersion+pad*/ + 22 * 8 /*IntPtrs up to SetTitle*/;
            const int OFFSET_ONSEND_NEW = OFFSET_ONRECV_NEW + 8;
            const int OFFSET_RECV_NEW = OFFSET_ONSEND_NEW + 8;   // host's injection fnptr (IntPtr) for incoming
            const int OFFSET_SEND_NEW = OFFSET_RECV_NEW + 8;     // host's injection fnptr (IntPtr) for outgoing
            IntPtr pluginPtr = (IntPtr) plugin;
            Marshal.WriteIntPtr( pluginPtr, OFFSET_ONRECV_NEW, Marshal.GetFunctionPointerForDelegate( _onReceiveNew ) );
            Marshal.WriteIntPtr( pluginPtr, OFFSET_ONSEND_NEW, Marshal.GetFunctionPointerForDelegate( _onSendNew ) );

            // Read host's IntPtr-variant injection fnptrs from the extended header. Older
            // CUO builds without these fields would have zero here — fall back to old API.
            IntPtr recvNewPtr = Marshal.ReadIntPtr( pluginPtr, OFFSET_RECV_NEW );
            IntPtr sendNewPtr = Marshal.ReadIntPtr( pluginPtr, OFFSET_SEND_NEW );
            if ( recvNewPtr != IntPtr.Zero )
                _sendToClientNew = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecvNewIntPtr>( recvNewPtr );
            if ( sendNewPtr != IntPtr.Zero )
                _sendToServerNew = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecvNewIntPtr>( sendNewPtr );
            InstallLog( $"Host injection fnptrs: Recv_new={recvNewPtr:X} Send_new={sendNewPtr:X}" );
        }

        private static int _sendNewCallCount;
        private static bool OnPacketSendNew( byte[] data, ref int length )
        {
            try
            {
                byte[] tmp = data;
                bool rv = OnPacketSendCore( ref tmp, ref length );
                if ( System.Threading.Interlocked.Increment( ref _sendNewCallCount ) <= 20 )
                {
                    InstallLog( $"OnPacketSendNew #{_sendNewCallCount}: id=0x{(data != null && data.Length > 0 ? data[0] : 0):X2} data.Len={(data == null ? -1 : data.Length)} len={length} → {rv}" );
                }
                return rv;
            }
            catch ( Exception ex )
            {
                Console.Error.WriteLine( $"[ClassicAssist] OnPacketSendNew swallowed {ex.GetType().Name}: {ex.Message}" );
                InstallLog( $"OnPacketSendNew swallowed {ex.GetType().Name} (id=0x{(data != null && data.Length > 0 ? data[0] : 0):X2} data.Len={(data == null ? -1 : data.Length)} len={length}): {ex.Message}" );
                return false;
            }
        }

        private static int _recvNewCallCount;
        private static bool OnPacketReceiveNew( byte[] data, ref int length )
        {
            try
            {
                byte[] tmp = data;
                int origLen = length;
                bool rv = OnPacketReceiveCore( ref tmp, ref length );

                // Filters that resize the packet (Array.Resize/=newArray) replace `tmp` with
                // a different array. `data` is the marshaller's [In, Out] managed copy and
                // is what gets written back to the unmanaged host buffer on return — only
                // its CONTENTS get copied back, never a replacement reference. So if a
                // filter reassigned the array, copy the new bytes into `data` so the host
                // sees them. Truncated to data.Length when the filter grew the packet
                // beyond what the marshalled buffer can hold (rare; NameOverride mutating
                // 0xC1 LocalizedMessage with a longer override name).
                if ( !ReferenceEquals( tmp, data ) && tmp != null && data != null )
                {
                    int copyLen = Math.Min( length, data.Length );
                    if ( copyLen > 0 ) Array.Copy( tmp, 0, data, 0, copyLen );
                    if ( length > data.Length )
                    {
                        InstallLog( $"OnPacketReceiveNew truncated id=0x{data[0]:X2} grew {origLen}→{length} > buffer {data.Length}; only {copyLen} bytes copied back" );
                        length = data.Length;
                    }
                }

                int n = System.Threading.Interlocked.Increment( ref _recvNewCallCount );
                if ( n <= 60 || length != origLen )
                {
                    InstallLog( $"OnPacketReceiveNew #{n}: id=0x{(data != null && data.Length > 0 ? data[0] : 0):X2} data.Len={(data == null ? -1 : data.Length)} len={origLen}{(length != origLen ? $"→{length}" : "")} → {rv}" );
                }
                return rv;
            }
            catch ( Exception ex )
            {
                Console.Error.WriteLine( $"[ClassicAssist] OnPacketReceiveNew swallowed {ex.GetType().Name}: {ex.Message}" );
                InstallLog( $"OnPacketReceiveNew swallowed {ex.GetType().Name} (id=0x{(data != null && data.Length > 0 ? data[0] : 0):X2} data.Len={(data == null ? -1 : data.Length)} len={length}): {ex.Message}" );
                return false;
            }
        }

        // Anything that may touch Dispatcher.UIThread (extension ctors, BaseViewModel.cctor,
        // AssistantOptions.Load → view-model load). Must run AFTER Avalonia's platform setup
        // has installed the Win32 dispatcher impl, otherwise the dispatcher singleton gets
        // created with the default ManagedDispatcherImpl and StartWithClassicDesktopLifetime
        // throws PlatformNotSupportedException trying to drive MainLoop.
        internal static void InitializePluginLate()
        {
            // First-run: extract bundled Modules.zip (IronPython stdlib + yapf)
            // into Modules/. WPF did this via the Updater process; we run as
            // a CoreCLR-embed plugin with no installer, so do it inline.
            try
            {
                string modulesDir = Path.Combine( StartupPath ?? Environment.CurrentDirectory, "Modules" );
                string modulesZip = Path.Combine( StartupPath ?? Environment.CurrentDirectory, "Modules.zip" );
                if ( File.Exists( modulesZip ) && Directory.Exists( modulesDir )
                     && !File.Exists( Path.Combine( modulesDir, "__future__.py" ) ) )
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory( modulesZip, modulesDir, overwriteFiles: true );
                }
            }
            catch ( Exception ex ) { Console.Error.WriteLine( $"Modules.zip extract: {ex}" ); }

            ClientPath = _getUOFilePath();
            if ( !Path.IsPathRooted( ClientPath ) )
            {
                ClientPath = Path.GetFullPath( ClientPath );
            }

            Art.Initialize( ClientPath );
            Hues.Initialize( ClientPath );
            Cliloc.Initialize( ClientPath );
            Skills.Initialize( ClientPath );
            Speech.Initialize( ClientPath );
            TileData.Initialize( ClientPath );
            Statics.Initialize( ClientPath );
            MapInfo.Initialize( ClientPath );

            ClassicAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault( a => a.FullName.StartsWith( "ClassicUO," ) );

            if ( ClassicAssembly != null )
            {
                CUOPath = Path.GetDirectoryName( ClassicAssembly.Location );
            }

            InitializeExtensions();
        }

        private static void OnFocusChanged( bool focus )
        {
            try { OnFocusChangedCore( focus ); }
            catch ( Exception ex ) { Console.Error.WriteLine( $"[ClassicAssist] OnFocusChanged swallowed {ex.GetType().Name}: {ex.Message}" ); }
        }

        private static void OnFocusChangedCore( bool focus )
        {
            if ( focus == _clientHasFocus )
            {
                return;
            }

            _clientHasFocus = focus;

            FocusChangedEvent?.Invoke( focus );
        }

        public static event dFocusChanged FocusChangedEvent;

        private static void OnTick()
        {
            try
            {
                while ( TickWorkQueue.Count > 0 )
                {
                    Action action = TickWorkQueue.Dequeue();

                    action?.Invoke();
                }
            }
            catch ( Exception e )
            {
                try { SentrySdk.CaptureException( e ); } catch { }
                try { Commands.SystemMessage( e.Message ); } catch { }
                try { Console.Error.WriteLine( $"[ClassicAssist] OnTick swallowed {e.GetType().Name}: {e.Message}" ); } catch { }
            }
        }

        private static void InitializeExtensions()
        {
            IEnumerable<Type> types = Assembly.GetExecutingAssembly().GetTypes().Where( t => typeof( IExtension ).IsAssignableFrom( t ) && t.IsClass );

            foreach ( Type type in types )
            {
                try
                {
                    IExtension instance = (IExtension) Activator.CreateInstance( type );
                    instance?.Initialize();
                }
                catch ( Exception e )
                {
                    Console.WriteLine( e.ToString() );
                }
            }
        }

        private static void OnMouse( int button, int wheel )
        {
            try { OnMouseCore( button, wheel ); }
            catch ( Exception ex ) { Console.Error.WriteLine( $"[ClassicAssist] OnMouse swallowed {ex.GetType().Name}: {ex.Message}" ); }
        }

        private static void OnMouseCore( int button, int wheel )
        {
            MouseOptions mouse = MouseOptions.None;

            if ( button > 0 )
            {
                mouse = MouseButtonToMouseOptions( button );
            }

            if ( wheel != 0 )
            {
                mouse = wheel < 0 ? MouseOptions.MouseWheelDown : MouseOptions.MouseWheelUp;

                if ( Options.CurrentOptions.LimitMouseWheelTrigger )
                {
                    TimeSpan diff = DateTime.Now - _lastMouseAction[(int) mouse];

                    if ( diff < TimeSpan.FromMilliseconds( Options.CurrentOptions.LimitMouseWheelTriggerMS ) )
                    {
                        return;
                    }
                }

                _lastMouseAction[(int) mouse] = DateTime.Now;
            }

            // MUST be Post not Invoke: this runs on CUO's UI thread (called every mouse event).
            // A synchronous Invoke would block CUO's thread waiting for Avalonia's UI thread,
            // which is itself frequently busy → CUO marked "Not Responding" by Windows.
            ModKey modifier = IntToModKey( _currentModifier );
            Dispatcher.UIThread.Post( () =>
            {
                try { HotkeyManager.GetInstance().OnMouseAction( mouse, modifier ); }
                catch ( Exception ex ) { Console.Error.WriteLine( $"[ClassicAssist] OnMouseAction swallowed {ex.GetType().Name}: {ex.Message}" ); }
            } );
        }

        private static bool OnHotkeyPressed( int key, int mod, bool pressed )
        {
            try { return OnHotkeyPressedCore( key, mod, pressed ); }
            catch ( Exception ex )
            {
                Console.Error.WriteLine( $"[ClassicAssist] OnHotkeyPressed swallowed {ex.GetType().Name}: {ex.Message}" );
                return false;
            }
        }

        private static bool OnHotkeyPressedCore( int key, int mod, bool pressed )
        {
            Key keys = SDLKeyToKeys( key );

            // Track current modifier state for mouse hotkey lookups. Avalonia has no global
            // Keyboard.IsKeyDown, but the host already forwards mod state on every keyboard
            // event (both press and release). Capture BEFORE the early-return so release
            // events clear the bits — otherwise modifiers latch forever.
            _currentModifier = mod;

            if ( !pressed )
            {
                return true;
            }

            bool noexecute = false;

            if ( Options.CurrentOptions.LimitHotkeyTrigger )
            {
                if ( _lastKeyAction.TryGetValue( keys, out DateTime lastAction ) )
                {
                    TimeSpan diff = DateTime.Now - lastAction;

                    if ( diff < TimeSpan.FromMilliseconds( Options.CurrentOptions.LimitHotkeyTriggerMS ) )
                    {
                        noexecute = true;
                    }
                }
            }

            if ( !noexecute )
            {
                HotkeyPressedEvent?.Invoke( key, mod, keys, IntToModKey( mod ) );
            }

            ( bool found, bool pass ) = HotkeyManager.GetInstance().OnHotkeyPressed( keys, IntToModKey( mod ), noexecute );

            if ( found && !noexecute )
            {
                _lastKeyAction[keys] = DateTime.Now;
            }

            return !pass;
        }

        public static event dClientClosing ClientClosing;

        internal static void InstallLog( string msg )
        {
            try
            {
                string pluginDir = Path.GetDirectoryName( typeof( Engine ).Assembly.Location ) ?? Path.GetTempPath();
                string line = $"[{DateTime.Now:O}] {msg}\n";
                try { File.AppendAllText( Path.Combine( pluginDir, "ClassicAssist.install.log" ), line ); } catch { }
                try { File.AppendAllText( Path.Combine( Path.GetTempPath(), "ClassicAssist.install.log" ), line ); } catch { }
            }
            catch { }
        }

        private static void OnClientClosing()
        {
            InstallLog( "OnClientClosing entered." );
            IsShuttingDown = true;
            // This delegate is invoked by CUO's .NET Framework host via a native function
            // pointer we handed it in PluginHeader. An exception escaping here crosses the
            // CLR boundary back into the host CLR — which the host doesn't (and can't)
            // catch, manifesting as the AccessViolationException seen at
            // ClassicUOHost.ClosingPlugin. Wrap each shutdown step independently so one
            // partial-init component (a VM whose collection was never populated, etc.)
            // can't take down the whole teardown.
            void Try( string label, Action a )
            {
                try { a(); }
                catch ( Exception ex ) { Console.Error.WriteLine( $"[ClassicAssist] OnClientClosing/{label} failed: {ex.GetType().Name}: {ex.Message}" ); }
            }

            Try( "ClientClosing event", () => ClientClosing?.Invoke() );
            Try( "Options.Save", () => Options.Save( Options.CurrentOptions ) );
            Try( "AssistantOptions.Save", () => AssistantOptions.Save() );
            Try( "Sentry.Close", () => SentrySdk.Close() );
            // Tear down the Avalonia lifetime so the UI thread exits. OnClientClosing runs
            // on CUO's thread; Dispatcher.UIThread.Post marshals to the UI thread. The Closing
            // handler on MainWindow checks IsShuttingDown to allow the close.
            Try( "Desktop.Shutdown", () =>
            {
                // Post (not Invoke): we're on CUO's closing thread; sync Invoke would block
                // waiting for the very dispatcher we're asking to shut down.
                Dispatcher.UIThread.Post( () =>
                {
                    if ( Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop )
                        desktop.Shutdown();
                } );
            } );
            InstallLog( "OnClientClosing returned cleanly." );
        }

        private static void OnPlayerPositionChanged( int x, int y, int z )
        {
            try
            {
                if ( Player != null )
                {
                    Player.X = x;
                    Player.Y = y;
                    Player.Z = z;
                }

                Items.RemoveByDistance( MAX_DISTANCE, x, y );
                Mobiles.RemoveByDistance( MAX_DISTANCE, x, y );

                Task.Run( () => { try { ScavengerManager.GetInstance().CheckArea?.Invoke(); } catch ( Exception ex ) { Console.Error.WriteLine( $"[ClassicAssist] Scavenger.CheckArea swallowed {ex.GetType().Name}: {ex.Message}" ); } } ).ConfigureAwait( false );
            }
            catch ( Exception ex ) { Console.Error.WriteLine( $"[ClassicAssist] OnPlayerPositionChanged swallowed {ex.GetType().Name}: {ex.Message}" ); }
        }

        public static Item GetOrCreateItem( int serial, int containerSerial = -1 )
        {
            Item item = Items.GetItem( serial );

            if ( item != null )
            {
                return item;
            }

            item = new Item( serial, containerSerial );

            if ( IncomingPacketHandlers.PropertyCache.TryGetValue( serial, out Property[] properties ) )
            {
                item.Properties = properties;
            }

            return item;
        }

        public static Mobile GetOrCreateMobile( int serial )
        {
            if ( Player?.Serial == serial )
            {
                return Player;
            }

            if ( Mobiles.GetMobile( serial, out Mobile mobile ) )
            {
                return mobile;
            }

            mobile = new Mobile( serial );

            if ( IncomingPacketHandlers.PropertyCache.TryGetValue( serial, out Property[] properties ) )
            {
                mobile.Properties = properties;
            }

            return mobile;
        }

        private static void Initialize()
        {
            StartupPath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );

            if ( StartupPath == null )
            {
                throw new InvalidOperationException();
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            PacketWaitEntries = new PacketWaitEntries();

            IncomingQueue = new ThreadQueue<Packet>( ProcessIncomingQueue );
            OutgoingQueue = new ThreadQueue<Packet>( ProcessOutgoingQueue );

            IncomingPacketHandlers.Initialize();
            OutgoingPacketHandlers.Initialize();

            IncomingPacketFilters.Initialize();
            OutgoingPacketFilters.Initialize();

            CommandsManager.Initialize();

            // NOTE: do NOT touch Dispatcher.UIThread here. Initialize() runs on whatever
            // thread Install was invoked from (CUO's bootstrap thread). The first thread to
            // access Dispatcher.UIThread becomes the dispatcher's owner, and only that thread
            // can drive Dispatcher.MainLoop later. We need our worker (_mainThread) to own
            // it, so StartupDispatcher gets assigned inside the AfterSetup callback below.

            AssistantOptions.Load();
        }

        private static void ProcessIncomingQueue( Packet packet )
        {
            if ( _incomingStopwatch == null )
            {
                _incomingStopwatch = new Stopwatch();
            }

            _incomingStopwatch.Reset();
            _incomingStopwatch.Start();

            string handlerName = "None";

            try
            {
                PacketReceivedEvent?.Invoke( packet.GetPacket(), packet.GetLength() );

                PacketHandler handler = IncomingPacketHandlers.GetHandler( packet.GetPacketID() );

                int length = _getPacketLength( packet.GetPacketID() );

                if ( handler != null )
                {
                    handlerName = handler.OnReceive.Method.Name;
                }

                handler?.OnReceive?.Invoke( new PacketReader( packet.GetPacket(), packet.GetLength(), length > 0 ) );

                PacketWaitEntries?.CheckWait( packet.GetPacket(), PacketDirection.Incoming );
            }
            catch ( Exception e )
            {
                try { Console.Error.WriteLine( $"[ClassicAssist] Incoming handler {handlerName} (id=0x{packet.GetPacketID():X2}) swallowed {e.GetType().Name}: {e.Message}" ); } catch { }
                try
                {
                    SentrySdk.CaptureException( e, scope =>
                    {
                        try { scope.SetExtra( "Packet", packet.GetPacket() ); } catch { }
                        try { scope.SetExtra( "Player", Player?.ToString() ?? "<null>" ); } catch { }
                        try { scope.SetExtra( "WorldItemCount", Items?.Count() ?? 0 ); } catch { }
                        try { scope.SetExtra( "WorldMobileCount", Mobiles?.Count() ?? 0 ); } catch { }
                    } );
                }
                catch { }
            }

            _incomingStopwatch.Stop();

            if ( _incomingStopwatch.ElapsedMilliseconds >= Options.CurrentOptions.SlowHandlerThreshold )
            {
                SlowHandlerEvent?.Invoke( PacketDirection.Incoming, handlerName, _incomingStopwatch.Elapsed );
            }
        }

        public static event dSlowHandler SlowHandlerEvent;

        private static void ProcessOutgoingQueue( Packet packet )
        {
            if ( _outgoingStopwatch == null )
            {
                _outgoingStopwatch = new Stopwatch();
            }

            _outgoingStopwatch.Reset();
            _outgoingStopwatch.Start();

            string handlerName = "None";

            try
            {
                PacketSentEvent?.Invoke( packet.GetPacket(), packet.GetLength() );

                PacketHandler handler = OutgoingPacketHandlers.GetHandler( packet.GetPacketID() );

                if ( handler != null )
                {
                    handlerName = handler.OnReceive.Method.Name;
                }

                int length = _getPacketLength( packet.GetPacketID() );

                handler?.OnReceive?.Invoke( new PacketReader( packet.GetPacket(), packet.GetLength(), length > 0 ) );

                PacketWaitEntries?.CheckWait( packet.GetPacket(), PacketDirection.Outgoing );
            }
            catch ( Exception e )
            {
                try { Console.Error.WriteLine( $"[ClassicAssist] Outgoing handler {handlerName} (id=0x{packet.GetPacketID():X2}) swallowed {e.GetType().Name}: {e.Message}" ); } catch { }
                try
                {
                    SentrySdk.CaptureException( e, scope =>
                    {
                        try { scope.SetExtra( "Packet", packet.GetPacket() ); } catch { }
                        try { scope.SetExtra( "Player", Player?.ToString() ?? "<null>" ); } catch { }
                        try { scope.SetExtra( "WorldItemCount", Items?.Count() ?? 0 ); } catch { }
                        try { scope.SetExtra( "WorldMobileCount", Mobiles?.Count() ?? 0 ); } catch { }
                    } );
                }
                catch { }
            }

            _outgoingStopwatch.Stop();

            if ( _outgoingStopwatch.ElapsedMilliseconds >= Options.CurrentOptions.SlowHandlerThreshold )
            {
                SlowHandlerEvent?.Invoke( PacketDirection.Outgoing, handlerName, _outgoingStopwatch.Elapsed );
            }
        }

        private static Assembly OnAssemblyResolve( object sender, ResolveEventArgs args )
        {
            string assemblyname = new AssemblyName( args.Name ).Name;

            string[] searchPaths = { StartupPath, RuntimeEnvironment.GetRuntimeDirectory() };

            if ( AssistantOptions.Assemblies?.Length > 0 )
            {
                searchPaths = searchPaths.Concat( GetAdditionalAssemblyPaths() ).ToArray();
            }

            if ( assemblyname.Contains( "Colletions" ) )
            {
                assemblyname = "System.Collections";
            }

            foreach ( string searchPath in searchPaths )
            {
                string fullPath = Path.Combine( searchPath, assemblyname + ".dll" );

                string culture = new AssemblyName( args.Name ).CultureName;

                if ( !File.Exists( fullPath ) )
                {
                    string culturePath = Path.Combine( searchPath, culture, assemblyname + ".dll" );

                    if ( File.Exists( culturePath ) )
                    {
                        fullPath = culturePath;
                    }
                    else
                    {
                        continue;
                    }
                }

                Assembly assembly = Assembly.LoadFrom( fullPath );

                return assembly;
            }

            return null;
        }

        private static string[] GetAdditionalAssemblyPaths()
        {
            return AssistantOptions.Assemblies == null
                ? Array.Empty<string>()
                : ( from assembly in AssistantOptions.Assemblies select Path.GetDirectoryName( assembly ) ).Distinct().ToArray();
        }

        public static void SetPlayer( PlayerMobile mobile )
        {
            Player = mobile;

            PlayerInitializedEvent?.Invoke( mobile );

            mobile.MobileStatusUpdated += ( status, newStatus ) =>
            {
                if ( !Options.CurrentOptions.UseDeathScreenWhilstHidden )
                {
                    return;
                }

                if ( newStatus.HasFlag( MobileStatus.Hidden ) )
                {
                    SendPacketToClient(
                        new MobileUpdate( mobile.Serial, mobile.ID == 0x191 ? 0x193 : 0x192, mobile.Hue, newStatus, mobile.X, mobile.Y, mobile.Z, mobile.Direction ) );
                }
            };

            // Disabled in the Avalonia port: the upstream manifest at
            // classicassist.azurewebsites.net/releases.json describes WPF
            // releases from Reetus/ClassicAssist, which would (a) pop a
            // misleading "new version available" gump pointing at the wrong
            // fork, and (b) if the updater is present on Windows, overwrite
            // the Avalonia install with a WPF release zip and brick it.
            // Re-enable once this port has its own releases.json manifest.
            // CheckGitHubVersion().ConfigureAwait( false );

            AbilitiesManager.GetInstance().Enabled = AbilityType.None;
            AbilitiesManager.GetInstance().ResendGump( AbilityType.None );

            Task.Run( async () =>
            {
                await Task.Delay( 3000 );

                if ( Connected && Player?.Backpack != null && Player?.Backpack?.Container == null )
                {
                    ObjectCommands.UseObject( Player?.Backpack );
                }

                MacroManager.GetInstance().Autostart();
            } );
        }

        private static async Task CheckGitHubVersion()
        {
            try
            {
                UpdaterSettings updaterSettings = UpdaterSettings.Load( StartupPath ?? Environment.CurrentDirectory );

                ChangelogEntry latestRelease = await Updater.GetLatestRelease( updaterSettings.InstallPrereleases );

                string latestVersion = latestRelease.Version;
                string localVersion = VersionHelpers.GetProductVersion( Path.Combine( StartupPath ?? Environment.CurrentDirectory, "ClassicAssist.dll" ) ).ToString();

                if ( VersionHelpers.IsVersionNewer( localVersion, latestVersion ) && VersionHelpers.IsVersionNewer( AssistantOptions.UpdateGumpVersion, latestVersion ) )
                {
                    string commitMessage = await Updater.GetUpdateText( updaterSettings.InstallPrereleases );
                    string donationAmount = await GetDonationsSummary();
                    StringBuilder donationMessage = new StringBuilder();

                    if ( !string.IsNullOrEmpty( donationAmount ) )
                    {
                        donationMessage.AppendLine( string.Format( Strings.Current_month_donations, DateTime.Now.ToString( "MMMM" ), donationAmount ) );
                        donationMessage.AppendLine();
                        donationMessage.AppendLine( $"<A HREF=\"https://www.paypal.me/reeeetus\">{Strings.Donate_Now}</A>" );
                    }

                    StringBuilder message = new StringBuilder();
                    message.AppendLine( Strings.ProductName );
                    message.AppendLine( $"{Strings.New_version_available_} <A HREF=\"https://github.com/Reetus/ClassicAssist/releases/tag/{latestVersion}\">{latestVersion}</A>" );
                    message.AppendLine();

                    if ( !string.IsNullOrEmpty( donationAmount ) )
                    {
                        message.AppendLine( donationMessage.ToString() );
                    }

                    message.AppendLine( commitMessage );
                    message.AppendLine( $"<A HREF=\"https://github.com/Reetus/ClassicAssist/releases\">{Strings.See_More}</A>" );

                    UpdateMessageGump gump = new UpdateMessageGump( WindowHandle, message.ToString(), latestVersion );
                    gump.SendGump();
                }
            }
            catch ( Exception )
            {
                // Squash all
            }
        }

        private static async Task<string> GetDonationsSummary()
        {
            HttpClient httpClient = new HttpClient();

            HttpResponseMessage response = await httpClient.GetAsync( "https://classicassist.azurewebsites.net/api/donations/summary" );

            if ( !response.IsSuccessStatusCode )
            {
                return null;
            }

            try
            {
                string json = await response.Content.ReadAsStringAsync();

                dynamic obj = JsonConvert.DeserializeObject<dynamic>( json );

                return obj?.amount;
            }
            catch ( Exception e )
            {
                SentrySdk.CaptureException( e );
                return null;
            }
        }

        public static void SendPacketToServer( byte[] packet, int length )
        {
            lock ( _serverSendLock )
            {
                while ( DateTime.Now < _nextPacketSendTime )
                {
                    Thread.Sleep( 1 );
                }

                InternalPacketSentEvent?.Invoke( packet, length );

                PacketWaitEntries?.CheckWait( packet, PacketDirection.Outgoing, true );

                if ( _getPacketLength != null )
                {
                    int expectedLength = _getPacketLength( packet[0] );

                    if ( expectedLength == -1 )
                    {
                        expectedLength = ( packet[1] << 8 ) | packet[2];
                    }

                    if ( length != expectedLength )
                    {
                        SentrySdk.CaptureMessage( $"Invalid packet length: {length} != {expectedLength}", scope =>
                        {
                            scope.SetExtra( "Packet", packet );
                            scope.SetExtra( "Length", length );
                            scope.SetExtra( "Direction", PacketDirection.Outgoing );
                            scope.SetExtra( "Expected Length", expectedLength );
                        } );

                        return;
                    }
                }

                ( byte[] data, int dataLength ) = Utility.CopyBuffer( packet, length );

                CrossClrSend( _sendToServerNew, _sendToServer, ref data, ref dataLength );

                _nextPacketSendTime = DateTime.Now + PACKET_SEND_DELAY;
            }
        }

        // Route host injection through the IntPtr-variant new API when available; the old
        // ref-byte[] path hangs the host on every cross-CLR call. Pin our managed buffer,
        // pass its address, let the host copy out whatever it needs.
        private static void CrossClrSend( OnPacketSendRecvNewIntPtr newFn, OnPacketSendRecv oldFn, ref byte[] data, ref int length )
        {
            if ( newFn != null )
            {
                System.Runtime.InteropServices.GCHandle h = System.Runtime.InteropServices.GCHandle.Alloc( data, System.Runtime.InteropServices.GCHandleType.Pinned );
                try
                {
                    IntPtr p = h.AddrOfPinnedObject();
                    newFn( p, ref length );
                }
                finally { h.Free(); }
            }
            else
            {
                oldFn?.Invoke( ref data, ref length );
            }
        }

        public static void SendPacketToClient( byte[] packet, int length, bool delay = true )
        {
            try
            {
                lock ( _clientSendLock )
                {
                    if ( delay )
                    {
                        while ( DateTime.Now < _nextPacketRecvTime )
                        {
                            Thread.Sleep( 1 );
                        }
                    }

                    InternalPacketReceivedEvent?.Invoke( packet, length );

                    PacketWaitEntries?.CheckWait( packet, PacketDirection.Incoming, true );

                    if ( _getPacketLength != null )
                    {
                        int expectedLength = _getPacketLength( packet[0] );

                        if ( expectedLength == -1 )
                        {
                            expectedLength = ( packet[1] << 8 ) | packet[2];
                        }

                        if ( length != expectedLength )
                        {
                            SentrySdk.CaptureMessage( $"Invalid packet length: {length} != {expectedLength}", scope =>
                            {
                                scope.SetExtra( "Packet", packet );
                                scope.SetExtra( "Length", length );
                                scope.SetExtra( "Direction", PacketDirection.Incoming );
                                scope.SetExtra( "Expected Length", expectedLength );
                            } );

                            return;
                        }
                    }

                    ( byte[] data, int dataLength ) = Utility.CopyBuffer( packet, length );

                    CrossClrSend( _sendToClientNew, _sendToClient, ref data, ref dataLength );

                    _nextPacketRecvTime = DateTime.Now + PACKET_RECV_DELAY;
                }
            }
            catch ( ThreadInterruptedException )
            {
                // Macro was interupted whilst we were waiting...
            }
        }

        public static void SendPacketToClient( PacketWriter packet )
        {
            byte[] data = packet.ToArray();

            SendPacketToClient( data, data.Length );
        }

        public static void SendPacketToClient( BasePacket basePacket, bool delay = true )
        {
            if ( basePacket.Direction != PacketDirection.Any && basePacket.Direction != PacketDirection.Incoming )
            {
                throw new InvalidOperationException( "Send packet wrong direction." );
            }

            byte[] data = basePacket.ToArray();

            SendPacketToClient( data, data.Length, delay );
        }

        public static void SendPacketToServer( PacketWriter packet )
        {
            byte[] data = packet.ToArray();

            SendPacketToServer( data, data.Length );
        }

        public static void SendPacketToServer( BasePacket basePacket )
        {
            if ( basePacket.Direction != PacketDirection.Any && basePacket.Direction != PacketDirection.Outgoing )
            {
                throw new InvalidOperationException( "Send packet wrong direction." );
            }

            byte[] data = basePacket.ToArray();

            if ( data == null )
            {
                return;
            }

            basePacket.ThrottleBeforeSend();

            SendPacketToServer( data, data.Length );
        }

        public static bool Move( Direction direction, bool run )
        {
            return _requestMove?.Invoke( (int) direction, run ) ?? false;
        }

        public static void UpdateWindowTitle()
        {
            UpdateWindowTitleEvent?.Invoke();
        }

        public static void SetTitle( string title = null )
        {
            if ( Options.CurrentOptions.SetUOTitle )
            {
                _setTitle?.Invoke( string.IsNullOrEmpty( title ) ? Player == null ? string.Empty : $"{Player.Name} ({CurrentShard?.Name})" : title );
            }
            else
            {
                _setTitle?.Invoke( string.Empty );
            }
        }

        public static void GetMapZ( int x, int y, out sbyte groundZ, out sbyte staticZ )
        {
            groundZ = staticZ = (sbyte) ( Player?.Z ?? 0 );

            if ( ClassicAssembly == null )
            {
                return;
            }

            PropertyInfo mapProperty = ClassicAssembly.GetType( "ClassicUO.Game.World" )?.GetProperty( "Map" );

            if ( mapProperty == null )
            {
                return;
            }

            object mapInstance = mapProperty.GetMethod.Invoke( mapProperty, null );

            MethodInfo getMapZMethod = mapInstance?.GetType().GetMethod( "GetMapZ" );

            if ( getMapZMethod == null )
            {
                return;
            }

            object[] parameters = { x, y, null, null };

            getMapZMethod.Invoke( mapInstance, parameters );

            groundZ = (sbyte) parameters[2];
            staticZ = (sbyte) parameters[3];
        }

        public static void LaunchUpdater()
        {
            // No-op in the Avalonia port. The updater downloaded release
            // zips listed by upstream Reetus/ClassicAssist's WPF manifest;
            // running it here would overwrite this install with a WPF build
            // that has no CoreCLR/PluginLoader bridge. The Updater project
            // also doesn't build on Linux/macOS (WinForms dep). Users update
            // manually until this port has its own release pipeline.
        }

        public static bool CheckOutgoingPreFilter( byte[] data )
        {
            if ( _outgoingPacketPreFilter.MatchFilterAll( data, out PacketFilterInfo[] pfis ) <= 0 )
            {
                return false;
            }

            foreach ( PacketFilterInfo pfi in pfis )
            {
                pfi.Action?.Invoke( data, pfi );
            }

            SentPacketFilteredEvent?.Invoke( data, data.Length );

            PacketWaitEntries.CheckWait( data, PacketDirection.Outgoing, true );

            return true;
        }

        #region ClassicUO Events

        private static bool OnPacketSend( ref byte[] data, ref int length )
        {
            try { return OnPacketSendCore( ref data, ref length ); }
            catch ( Exception ex )
            {
                Console.Error.WriteLine( $"[ClassicAssist] OnPacketSend swallowed {ex.GetType().Name}: {ex.Message}" );
                InstallLog( $"OnPacketSend swallowed {ex.GetType().Name} (id=0x{(data != null && data.Length > 0 ? data[0] : 0):X2} data.Len={(data == null ? -1 : data.Length)} len={length}): {ex.Message}" );
                return false;
            }
        }

        // Bisecting which part of the receive chain hangs CUO at "Entering Britannia".
        //   STAGE 0 (true no-op):       confirmed CUO works.
        //   STAGE 1 (enqueue-only):     enqueues to worker, skips inline filter chain.
        //   STAGE 2 (full):             original WPF logic.
        // Bump this to 2 once stage 1 verifies clean.
        private const int PACKET_PROCESSING_STAGE = 2;

        private static bool OnPacketSendCore( ref byte[] data, ref int length )
        {
            if ( PACKET_PROCESSING_STAGE == 0 ) return true;
            if ( data.Length == 0 )
            {
                return false;
            }

            if ( PACKET_PROCESSING_STAGE == 1 )
            {
                try { OutgoingQueue.Enqueue( new Packet( data, length ) ); }
                catch ( Exception ex ) { Console.Error.WriteLine( $"[ClassicAssist] OutgoingQueue.Enqueue failed: {ex.GetType().Name}: {ex.Message}" ); }
                return true;
            }

            // Cross-CLR plumbing: when the host (.NET Framework) passes us `ref byte[]`,
            // the array reference points at the host's GC heap. We cannot safely:
            //   (a) read past whatever Length CoreCLR sees here (may be a misread of the foreign MT),
            //   (b) reassign `data` to a CoreCLR-allocated byte[] — host's Array.Copy will trip on
            //       the foreign MethodTable element-size and throw "Source array was not long enough".
            // Until we add a loader-side byte[] marshalling trampoline, bail out cleanly when the
            // view looks inconsistent so we don't crash the host.
            if ( data.Length < length )
            {
                return false;
            }

            bool filter = false;

            if ( CommandsManager.IsSpeechPacket( data[0] ) )
            {
                filter = CommandsManager.CheckCommand( data, length );
            }

            if ( CheckOutgoingPreFilter( data ) )
            {
                return false;
            }

            if ( OutgoingPacketFilters.CheckPacket( ref data, ref length ) )
            {
                SentPacketFilteredEvent?.Invoke( data, data.Length );

                return false;
            }

            OutgoingQueue.Enqueue( new Packet( data, length ) );

            // ReSharper disable once InvertIf
            if ( _outgoingPacketPostFilter.MatchFilterAll( data, out PacketFilterInfo[] pfisPost ) > 0 )
            {
                foreach ( PacketFilterInfo pfi in pfisPost )
                {
                    pfi.Action?.Invoke( data, pfi );
                }

                SentPacketFilteredEvent?.Invoke( data, data.Length );

                PacketWaitEntries.CheckWait( data, PacketDirection.Outgoing, true );

                return false;
            }

            return !filter;
        }

        private static IntPtr _windowHandle;

        public static IntPtr WindowHandle
        {
            get
            {
                if ( _windowHandle != IntPtr.Zero )
                {
                    return _windowHandle;
                }

                // HWND is a Windows concept; on Linux/macOS callers must degrade gracefully.
                if ( !OperatingSystem.IsWindows() )
                {
                    return IntPtr.Zero;
                }

                // Under the current CUO bootstrap split (see [[cuo-bootstrap-split]])
                // PluginHeader.HWND is populated as 0 — the native cuo.dll owns the
                // SDL window and doesn't surface its handle back through the header.
                // We can't just use Process.MainWindowHandle because the Avalonia
                // ClassicAssist window is ALSO a top-level window on this process
                // and Windows may return that one. Enumerate the process's top-level
                // windows and find the SDL one by class name.
                try
                {
                    IntPtr found = FindSdlWindow();

                    if ( found != IntPtr.Zero )
                    {
                        _windowHandle = found;
                        return _windowHandle;
                    }

                    // Last-resort fallback. May grab the wrong window (Avalonia) but
                    // better than zero for callers that don't care which window.
                    IntPtr h = Process.GetCurrentProcess().MainWindowHandle;

                    if ( h != IntPtr.Zero )
                    {
                        _windowHandle = h;
                    }

                    return h;
                }
                catch
                {
                    return IntPtr.Zero;
                }
            }
            private set => _windowHandle = value;
        }

        private static IntPtr FindSdlWindow()
        {
            uint pid = (uint) Process.GetCurrentProcess().Id;
            IntPtr result = IntPtr.Zero;

            NativeMethods.EnumWindows( ( hWnd, lParam ) =>
            {
                if ( !NativeMethods.IsWindowVisible( hWnd ) )
                {
                    return true;
                }

                NativeMethods.GetWindowThreadProcessId( hWnd, out uint windowPid );

                if ( windowPid != pid )
                {
                    return true;
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder( 256 );
                NativeMethods.GetClassName( hWnd, sb, sb.Capacity );
                string cls = sb.ToString();

                // SDL2 on Windows uses the "SDL_app" window class by default. If the
                // build customizes it, anything starting with "SDL" is a strong signal.
                if ( cls.StartsWith( "SDL", StringComparison.OrdinalIgnoreCase ) )
                {
                    result = hWnd;
                    return false; // stop enumeration
                }

                return true;
            }, IntPtr.Zero );

            return result;
        }

        public static ThreadQueue<Packet> IncomingQueue { get; set; }

        public static ThreadQueue<Packet> OutgoingQueue { get; set; }
        public static bool InternalTarget { get; set; }
        public static int InternalTargetSerial { get; set; }
        public static Trade Trade { get; set; } = new Trade();

        private static bool OnPacketReceive( ref byte[] data, ref int length )
        {
            try { return OnPacketReceiveCore( ref data, ref length ); }
            catch ( Exception ex )
            {
                Console.Error.WriteLine( $"[ClassicAssist] OnPacketReceive swallowed {ex.GetType().Name}: {ex.Message}" );
                InstallLog( $"OnPacketReceive swallowed {ex.GetType().Name} (id=0x{(data != null && data.Length > 0 ? data[0] : 0):X2} len={length}): {ex.Message}" );
                return false;
            }
        }

        private static bool OnPacketReceiveCore( ref byte[] data, ref int length )
        {
            if ( PACKET_PROCESSING_STAGE == 0 ) return true;
            if ( data.Length == 0 )
            {
                return false;
            }

            if ( PACKET_PROCESSING_STAGE == 1 )
            {
                try { IncomingQueue.Enqueue( new Packet( data, length ) ); }
                catch ( Exception ex ) { Console.Error.WriteLine( $"[ClassicAssist] IncomingQueue.Enqueue failed: {ex.GetType().Name}: {ex.Message}" ); }
                return true;
            }

            // See OnPacketSendCore for the cross-CLR rationale; bail without touching `data`.
            if ( data.Length < length )
            {
                return false;
            }

            if ( _incomingPacketFilter.MatchFilterAll( data, out PacketFilterInfo[] pfis ) > 0 )
            {
                foreach ( PacketFilterInfo pfi in pfis )
                {
                    pfi.Action?.Invoke( data, pfi );
                }

                ReceivedPacketFilteredEvent?.Invoke( data, length );

                PacketWaitEntries.CheckWait( data, PacketDirection.Incoming, true );

                return false;
            }

            if ( IncomingPacketFilters.CheckPacket( ref data, ref length ) )
            {
                ReceivedPacketFilteredEvent?.Invoke( data, length );

                return false;
            }

            IncomingQueue.Enqueue( new Packet( data, length ) );

            return true;
        }

        public static Direction GetSequence( int sequence )
        {
            return (Direction) Volatile.Read( ref _sequenceList[sequence] );
        }

        public static void SetSequence( int sequence, Direction direction )
        {
            _sequenceList[sequence] = (int) direction;
        }

        private static void OnConnected()
        {
            try
            {
                Connected = true;
                ConnectedEvent?.Invoke();
            }
            catch ( Exception ex ) { Console.Error.WriteLine( $"[ClassicAssist] OnConnected swallowed {ex.GetType().Name}: {ex.Message}" ); }
        }

        private static void OnDisconnected()
        {
            try
            {
                Connected = false;
                Items.Clear();
                Mobiles.Clear();
                Player = null;
                DisconnectedEvent?.Invoke();
            }
            catch ( Exception ex ) { Console.Error.WriteLine( $"[ClassicAssist] OnDisconnected swallowed {ex.GetType().Name}: {ex.Message}" ); }
        }

        #endregion
    }
}
