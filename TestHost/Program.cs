using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ClassicAssist.UI.Views;

namespace TestHost
{
    class Program
    {
        static bool s_interactive;
        static bool s_inspectCheckbox;
        static bool s_inspectTree;
        static bool s_inspectOptions;
        static bool s_inspectSkills;
        static bool s_inspectOrganizer;
        static bool s_inspectVendor;
        static bool s_inspectPublicMacros;
        static bool s_inspectAbout;
        static bool s_inspectMacrosHover;
        static bool s_inspectScreenshotHover;
        static bool s_inspectDebugPackets;
        static bool s_showGif;
        static int s_autoExitSeconds;

        [STAThread]
        public static void Main( string[] args )
        {
            s_interactive = Array.Exists( args, a => a == "--interactive" || a == "-i" );
            s_inspectCheckbox = Array.Exists( args, a => a == "--inspect-checkbox" );
            s_inspectTree = Array.Exists( args, a => a == "--inspect-tree" );
            s_inspectOptions = Array.Exists( args, a => a == "--inspect-options" );
            s_inspectSkills = Array.Exists( args, a => a == "--inspect-skills" );
            s_inspectOrganizer = Array.Exists( args, a => a == "--inspect-organizer" );
            s_inspectVendor = Array.Exists( args, a => a == "--inspect-vendor" );
            s_inspectPublicMacros = Array.Exists( args, a => a == "--inspect-public-macros" );
            s_inspectAbout = Array.Exists( args, a => a == "--inspect-about" );
            s_inspectMacrosHover = Array.Exists( args, a => a == "--inspect-macros-hover" );
            s_inspectScreenshotHover = Array.Exists( args, a => a == "--inspect-screenshot-hover" );
            s_inspectDebugPackets = Array.Exists( args, a => a == "--inspect-debug-packets" );
            s_showGif = Array.Exists( args, a => a == "--show-gif" );
            if ( s_inspectCheckbox || s_inspectTree || s_inspectOptions || s_inspectSkills || s_inspectOrganizer || s_inspectVendor || s_inspectPublicMacros || s_inspectAbout || s_inspectMacrosHover || s_inspectScreenshotHover || s_inspectDebugPackets ) s_interactive = true;
            for ( int i = 0; i < args.Length - 1; i++ )
                if ( args[i] == "--autoexit" && int.TryParse( args[i + 1], out int n ) ) s_autoExitSeconds = n;
            try
            {
                Console.WriteLine( "[TestHost] Starting Avalonia application..." );
                BuildAvaloniaApp().StartWithClassicDesktopLifetime( args );
            }
            catch ( InvalidOperationException ex ) when ( ex.Message.Contains( "Dispatcher" ) )
            {
                // Harmless teardown race: a window ctor's async continuation lands after the
                // dispatcher has stopped. The smoke summary has already printed; suppress.
            }
            catch ( Exception ex )
            {
                Console.WriteLine( $"[TestHost] FATAL: {ex}" );
                throw;
            }
        }

        static void BootstrapEngineStubs()
        {
            // Engine.StartupPath — many subsystems Path.Combine against this
            var outDir = System.IO.Path.GetDirectoryName( typeof( MainWindow ).Assembly.Location );
            Assistant.Engine.StartupPath = outDir;

            // Engine.Player — AliasCommands.GetPlayerAliases dereferences .Serial
            Assistant.Engine.Player = new ClassicAssist.UO.Objects.PlayerMobile( 0x00000001 );

            // AssistantOptions.BackupOptions — BackupSettingsWindowViewModel reads it in ctor
            if ( ClassicAssist.Data.AssistantOptions.BackupOptions == null )
                ClassicAssist.Data.AssistantOptions.BackupOptions = new ClassicAssist.Data.Backup.BackupOptions();

            // UO data paths — Hues / Cliloc Lazy<> initializers Path.Combine(_dataPath, ...).
            // Prefer TestHost/uo-data/ (gitignored — drop hues.mul + Cliloc.enu there for full coverage);
            // fall back to the output dir so Path.Combine still succeeds and we get a clean
            // FileNotFoundException SKIP rather than an NRE if the files aren't present.
            var repoTestHost = System.IO.Path.GetFullPath( System.IO.Path.Combine( outDir, "..", "..", "..", "uo-data" ) );
            var siblingData = System.IO.Path.Combine( outDir, "uo-data" );
            string dataPath = outDir;
            if ( System.IO.Directory.Exists( repoTestHost ) ) dataPath = repoTestHost;
            else if ( System.IO.Directory.Exists( siblingData ) ) dataPath = siblingData;
            Console.WriteLine( $"[TestHost] UO data path: {dataPath}" );
            ClassicAssist.UO.Data.Hues.Initialize( dataPath );
            ClassicAssist.UO.Data.Cliloc.Initialize( dataPath );

            // If a settings.json is present in uo-data/, stage it into the Profiles dir so
            // Options.Load picks it up. Done at bootstrap so the data is in place before
            // the LoadProfileIfAvailable() call in interactive mode runs against live VMs.
            try
            {
                var srcSettings = System.IO.Path.Combine( dataPath, "settings.json" );
                if ( System.IO.File.Exists( srcSettings ) )
                {
                    var profilesDir = System.IO.Path.Combine( outDir, "Profiles" );
                    System.IO.Directory.CreateDirectory( profilesDir );
                    var dstSettings = System.IO.Path.Combine( profilesDir, "settings.json" );
                    System.IO.File.Copy( srcSettings, dstSettings, overwrite: true );
                    Console.WriteLine( $"[TestHost] Staged profile: {srcSettings} -> {dstSettings}" );
                }
            }
            catch ( Exception ex ) { Console.WriteLine( $"[TestHost] (profile stage warning: {ex.Message})" ); }
        }

        // Called AFTER MainWindow ctor (so BaseViewModel.Instances is populated) but BEFORE
        // the window is shown. Loading here keeps the UI thread off the file I/O during
        // first paint while still hydrating the VMs that drive the visible tabs.
        static void LoadProfileIfAvailable()
        {
            try
            {
                var profilesDir = System.IO.Path.Combine( Assistant.Engine.StartupPath, "Profiles" );
                var settingsPath = System.IO.Path.Combine( profilesDir, ClassicAssist.Data.Options.DEFAULT_SETTINGS_FILENAME );
                if ( !System.IO.File.Exists( settingsPath ) )
                {
                    Console.WriteLine( $"[TestHost] No staged profile at {settingsPath}; skipping Options.Load." );
                    return;
                }
                ClassicAssist.Data.AssistantOptions.LastProfile = ClassicAssist.Data.Options.DEFAULT_SETTINGS_FILENAME;
                ClassicAssist.Data.Options.Load( ClassicAssist.Data.Options.DEFAULT_SETTINGS_FILENAME, ClassicAssist.Data.Options.CurrentOptions );
                Console.WriteLine( "[TestHost] Profile loaded into VM instances." );
            }
            catch ( Exception ex ) { Console.WriteLine( $"[TestHost] Profile load failed: {ex.Message}" ); }
        }

        static bool IsMissingUoDataFile( Exception ex )
        {
            // FileNotFoundException for hues.mul / Cliloc.enu means we reached real load logic —
            // the window itself constructed fine, the UO client data just isn't present.
            if ( ex is System.IO.FileNotFoundException fnf )
            {
                var name = fnf.FileName ?? string.Empty;
                return name.EndsWith( "hues.mul", StringComparison.OrdinalIgnoreCase ) ||
                       name.IndexOf( "Cliloc.", StringComparison.OrdinalIgnoreCase ) >= 0;
            }
            return false;
        }

        static void SmokeTestAllWindows()
        {
            try { BootstrapEngineStubs(); }
            catch ( Exception ex ) { Console.WriteLine( $"[TestHost] (bootstrap warning: {ex.Message})" ); }

            var asm = typeof( MainWindow ).Assembly;
            int ok = 0, fail = 0, skipped = 0;
            foreach ( var t in asm.GetTypes() )
            {
                if ( t.IsAbstract || !typeof( Window ).IsAssignableFrom( t ) || t == typeof( MainWindow ) )
                    continue;
                var ctor = t.GetConstructor( Type.EmptyTypes );
                if ( ctor == null )
                    continue;
                try
                {
                    var w = (Window) ctor.Invoke( null );
                    ok++;
                }
                catch ( Exception ex )
                {
                    var inner = ex.InnerException ?? ex;
                    if ( IsMissingUoDataFile( inner ) )
                    {
                        skipped++;
                        Console.WriteLine( $"[TestHost] SKIP {t.Name}: needs UO data file ({( (System.IO.FileNotFoundException) inner ).FileName})" );
                        continue;
                    }
                    fail++;
                    Console.WriteLine( $"[TestHost] FAIL {t.Name}: {inner.GetType().Name}: {inner.Message}" );
                    Console.WriteLine( $"    at {inner.StackTrace?.Split( '\n' )?[0]?.Trim()}" );
                }
            }
            Console.WriteLine( $"[TestHost] Smoke test: {ok} ok, {skipped} skipped (data unavailable), {fail} failed" );
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .AfterSetup( builder =>
                {
                    Console.WriteLine( "[TestHost] Avalonia setup complete, creating MainWindow..." );

                    if ( builder.Instance?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
                    {
                        try
                        {
                            // BootstrapEngineStubs must run BEFORE MainWindow ctor: e.g.
                            // MacrosCodeTextEditor.Grid_Initialized derefs Engine.StartupPath
                            // during initialization. Safe to call in both modes; the smoke
                            // path also calls it from SmokeTestAllWindows but it's idempotent.
                            try { BootstrapEngineStubs(); }
                            catch ( Exception ex ) { Console.WriteLine( $"[TestHost] (bootstrap warning: {ex.Message})" ); }

                            if ( s_showGif )
                            {
                                var gif = new GIFRecorderWindow();
                                desktop.MainWindow = gif;
                                Console.WriteLine( "[TestHost] --show-gif: GIFRecorderWindow is the MainWindow. Drag by the title bar; click Play to record, Stop, then Save (writes to <output>/Screenshots/)." );
                                return;
                            }

                            var window = new MainWindow();
                            desktop.MainWindow = window;
                            Console.WriteLine( "[TestHost] MainWindow created successfully!" );

                            if ( s_interactive )
                            {
                                // Load profile AFTER MainWindow ctor so BaseViewModel.Instances
                                // is populated — Options.Load iterates those to deserialize
                                // per-VM state. Still pre-Show, so first paint happens with
                                // data already hydrated.
                                LoadProfileIfAvailable();
                                if ( s_inspectCheckbox )
                                {
                                    window.Opened += ( _, _ ) =>
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post( () => InspectCheckBoxes( window ),
                                            Avalonia.Threading.DispatcherPriority.Background );
                                    };
                                }
                                if ( s_inspectTree )
                                {
                                    window.Opened += ( _, _ ) =>
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post( () => InspectTreeView( window ),
                                            Avalonia.Threading.DispatcherPriority.Background );
                                    };
                                }
                                if ( s_inspectOrganizer )
                                {
                                    window.Opened += ( _, _ ) =>
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post( () => InspectOrganizer( window ),
                                            Avalonia.Threading.DispatcherPriority.Background );
                                    };
                                }
                                if ( s_inspectSkills )
                                {
                                    window.Opened += ( _, _ ) =>
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post( () => InspectSkills( window ),
                                            Avalonia.Threading.DispatcherPriority.Background );
                                    };
                                }
                                if ( s_inspectVendor )
                                {
                                    window.Opened += ( _, _ ) =>
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post( () => InspectVendor( window ),
                                            Avalonia.Threading.DispatcherPriority.Background );
                                    };
                                }
                                if ( s_inspectMacrosHover )
                                {
                                    window.Opened += ( _, _ ) =>
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post( () => InspectMacrosHover( window ),
                                            Avalonia.Threading.DispatcherPriority.Background );
                                    };
                                }
                                if ( s_inspectScreenshotHover )
                                {
                                    window.Opened += ( _, _ ) =>
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post( () => InspectScreenshotHover( window ),
                                            Avalonia.Threading.DispatcherPriority.Background );
                                    };
                                }
                                if ( s_inspectAbout )
                                {
                                    window.Opened += ( _, _ ) =>
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post( () => InspectAbout( window ),
                                            Avalonia.Threading.DispatcherPriority.Background );
                                    };
                                }
                                if ( s_inspectPublicMacros )
                                {
                                    window.Opened += ( _, _ ) =>
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post( () => InspectPublicMacros( window ),
                                            Avalonia.Threading.DispatcherPriority.Background );
                                    };
                                }
                                if ( s_inspectOptions )
                                {
                                    window.Opened += ( _, _ ) =>
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post( () => InspectOptions( window ),
                                            Avalonia.Threading.DispatcherPriority.Background );
                                    };
                                }
                                if ( s_inspectDebugPackets )
                                {
                                    window.Opened += ( _, _ ) =>
                                    {
                                        Avalonia.Threading.Dispatcher.UIThread.Post( () => InspectDebugPackets( window ),
                                            Avalonia.Threading.DispatcherPriority.Background );
                                    };
                                }
                                Console.WriteLine( "[TestHost] Interactive mode — MainWindow will be shown. Close it to exit." );
                                if ( s_autoExitSeconds > 0 )
                                {
                                    Console.WriteLine( $"[TestHost] Auto-exit in {s_autoExitSeconds}s." );
                                    var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds( s_autoExitSeconds ) };
                                    timer.Tick += ( _, _ ) => { timer.Stop(); Console.WriteLine( "[TestHost] Auto-exit firing." ); desktop.Shutdown( 0 ); };
                                    timer.Start();
                                }
                            }
                            else
                            {
                                SmokeTestAllWindows();
                                // Auto-exit after smoke test so TestHost can run unattended.
                                // Pass --interactive (or -i) for visual inspection of MainWindow.
                                desktop.Shutdown( 0 );
                            }
                        }
                        catch ( Exception ex )
                        {
                            Console.WriteLine( $"[TestHost] ERROR creating MainWindow: {ex}" );

                            // Fallback: show a simple window with the error
                            var fallback = new Window
                            {
                                Title = "TestHost - Error",
                                Width = 800,
                                Height = 400,
                                Content = new TextBlock
                                {
                                    Text = $"Failed to create MainWindow:\n\n{ex}",
                                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                                    Margin = new Thickness( 20 )
                                }
                            };
                            desktop.MainWindow = fallback;
                        }
                    }
                } );
        }

        static void InspectCheckBoxes( Avalonia.Controls.Window window )
        {
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabControl tc && tc.Items.Count > 1 )
                {
                    for ( int i = 0; i < tc.Items.Count; i++ )
                        if ( tc.Items[i] is Avalonia.Controls.TabItem ti && ti.Header?.ToString()?.Contains( "Macros" ) == true )
                            { tc.SelectedIndex = i; break; }
                    break;
                }
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Console.WriteLine( "[Inspect] ===== CheckBox dump =====" );
            int count = 0;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is not Avalonia.Controls.CheckBox cb ) continue;
                if ( !cb.IsVisible || cb.Bounds.Height <= 0 ) continue;
                string content = cb.Content?.ToString() ?? "";
                if ( string.IsNullOrEmpty( content ) ) continue;
                if ( cb.IsChecked != true ) continue;
                count++;
                if ( count > 3 ) break;
                string label = cb.Content?.ToString() ?? "(no content)";
                Console.WriteLine( $"[Inspect] -- CheckBox '{label}' --" );
                Console.WriteLine( $"[Inspect]   Bounds={cb.Bounds} DesiredSize={cb.DesiredSize}" );
                Console.WriteLine( $"[Inspect]   Height={cb.Height} MinHeight={cb.MinHeight} MaxHeight={cb.MaxHeight}" );
                Console.WriteLine( $"[Inspect]   Padding={cb.Padding} Margin={cb.Margin}" );
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( cb ) )
                {
                    var c = d as Avalonia.Controls.Control;
                    string name = c?.Name ?? "";
                    string extra = "";
                    if ( c != null )
                    {
                        extra = $" Margin={c.Margin}";
                        if ( c is Avalonia.Controls.Primitives.TemplatedControl tc )
                            extra += $" Padding={tc.Padding} MinHeight={tc.MinHeight} Height={tc.Height}";
                        else if ( c is Avalonia.Controls.Decorator dec )
                            extra += $" MinHeight={dec.MinHeight} Height={dec.Height}";
                        else if ( c is Avalonia.Layout.Layoutable l )
                            extra += $" MinHeight={l.MinHeight} Height={l.Height}";
                    }
                    Console.WriteLine( $"[Inspect]   {d.GetType().Name}#{name} Bounds={d.Bounds}{extra}" );
                }
            }
            Console.WriteLine( $"[Inspect] ===== {count} CheckBox(es) inspected =====" );
        }

        static void InspectOptions( Avalonia.Controls.Window window )
        {
            // Switch to Options tab
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabControl tc && tc.Items.Count > 1 )
                {
                    for ( int i = 0; i < tc.Items.Count; i++ )
                        if ( tc.Items[i] is Avalonia.Controls.TabItem ti && ti.Header?.ToString()?.Contains( "Options" ) == true )
                            { tc.SelectedIndex = i; break; }
                    break;
                }
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();

            Console.WriteLine( "[Inspect] ===== Options tab dump =====" );

            // HorizontalHeaderedContentControl instances
            int hccCount = 0;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                var typeName = v.GetType().Name;
                if ( typeName != "HorizontalHeaderedContentControl" && typeName != "HorizontalHeaderedComboBox" && typeName != "HorizontalHeaderedTextBox" )
                    continue;
                hccCount++;
                if ( hccCount > 3 ) break;
                var tc = v as Avalonia.Controls.Primitives.TemplatedControl;
                var hc = v as Avalonia.Controls.Primitives.HeaderedContentControl;
                var cc = v as Avalonia.Controls.ContentControl;
                Console.WriteLine( $"[Inspect] -- {typeName} #{hccCount} --" );
                Console.WriteLine( $"[Inspect]   BaseType={v.GetType().BaseType?.Name}" );
                Console.WriteLine( $"[Inspect]   Bounds={(v as Avalonia.Controls.Control)?.Bounds}" );
                Console.WriteLine( $"[Inspect]   Template={tc?.Template?.GetType().Name ?? "<null>"}" );
                Console.WriteLine( $"[Inspect]   Header={hc?.Header}" );
                Console.WriteLine( $"[Inspect]   Content={cc?.Content?.GetType().Name ?? "<null>"}" );
                int depth = 0;
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( v ) )
                {
                    if ( depth++ > 12 ) break;
                    var c = d as Avalonia.Controls.Control;
                    string name = c?.Name ?? "";
                    string text = "";
                    if ( d is Avalonia.Controls.TextBlock tb ) text = $" Text='{tb.Text}' Foreground={tb.Foreground}";
                    if ( d is Avalonia.Controls.Presenters.ContentPresenter cp ) text = $" Content={cp.Content?.GetType().Name ?? "<null>"} Child={cp.Child?.GetType().Name ?? "<null>"}";
                    Console.WriteLine( $"[Inspect]     {d.GetType().Name}#{name} Bounds={d.Bounds}{text}" );
                }
            }
            Console.WriteLine( $"[Inspect] ===== {hccCount} headered control(s) inspected =====" );

            // Also dump the outer HeaderedContentControl sections (General, Target, ...)
            // and a few TextBoxes — these are where the missing "header strip / box / line spacing"
            // complaints likely originate.
            int hccBaseCount = 0;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is not Avalonia.Controls.Primitives.HeaderedContentControl hcc ) continue;
                if ( v.GetType().Name != "HeaderedContentControl" ) continue; // exclude subclasses
                hccBaseCount++;
                if ( hccBaseCount > 3 ) break;
                Console.WriteLine( $"[Inspect] -- HeaderedContentControl (base) #{hccBaseCount} Header='{hcc.Header}' --" );
                var tc = v as Avalonia.Controls.Primitives.TemplatedControl;
                Console.WriteLine( $"[Inspect]   Template={tc?.Template?.GetType().Name ?? "<null>"}" );
                Console.WriteLine( $"[Inspect]   Bounds={hcc.Bounds} BorderBrush={tc?.BorderBrush} BorderThickness={tc?.BorderThickness}" );
                int d = 0;
                foreach ( var ch in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( v ) )
                {
                    if ( d++ > 8 ) break;
                    var ctl = ch as Avalonia.Controls.Control;
                    string name = ctl?.Name ?? "";
                    string extra = "";
                    if ( ch is Avalonia.Controls.Border br ) extra = $" Background={br.Background} BorderBrush={br.BorderBrush} BorderThickness={br.BorderThickness}";
                    Console.WriteLine( $"[Inspect]     {ch.GetType().Name}#{name} Bounds={ch.Bounds}{extra}" );
                }
            }
            Console.WriteLine( $"[Inspect] -- TextBoxes on Options tab --" );
            int tbCount = 0;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is not Avalonia.Controls.TextBox tb ) continue;
                if ( tb.Bounds.Height <= 0 ) continue;
                tbCount++;
                if ( tbCount > 4 ) break;
                Console.WriteLine( $"[Inspect]   TextBox Text='{tb.Text}' Bounds={tb.Bounds} MinHeight={tb.MinHeight} Padding={tb.Padding}" );
            }
        }

        static void InspectPublicMacros( Avalonia.Controls.Window window )
        {
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabControl tc && tc.Items.Count > 1 )
                {
                    for ( int i = 0; i < tc.Items.Count; i++ )
                        if ( tc.Items[i] is Avalonia.Controls.TabItem ti && ti.Header?.ToString()?.Contains( "Macros" ) == true && ti.Header?.ToString()?.Contains( "Public" ) == true )
                            { tc.SelectedIndex = i; break; }
                    if ( tc.SelectedItem == null || ( tc.SelectedItem is Avalonia.Controls.TabItem si && si.Header?.ToString()?.Contains( "Public" ) != true ) )
                    {
                        for ( int i = 0; i < tc.Items.Count; i++ )
                            if ( tc.Items[i] is Avalonia.Controls.TabItem ti2 && ti2.Header?.ToString()?.Contains( "Public" ) == true )
                                { tc.SelectedIndex = i; break; }
                    }
                    break;
                }
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Console.WriteLine( "[Inspect] ===== Public Macros header dump =====" );
            int cbCount = 0;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v.GetType().Name != "ClearableComboBox" ) continue;
                cbCount++;
                var cb = v as Avalonia.Controls.ComboBox;
                Console.WriteLine( $"[Inspect] -- ClearableComboBox #{cbCount} Bounds={cb.Bounds} HA={cb.HorizontalAlignment} --" );
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( v ) )
                {
                    var c = d as Avalonia.Controls.Control;
                    string name = c?.Name ?? "";
                    string text = "";
                    if ( d is Avalonia.Controls.Image img ) text = $" Source={img.Source} W={img.Width} H={img.Height}";
                    if ( d is Avalonia.Controls.Button btn ) text = $" Name={btn.Name} HA={btn.HorizontalAlignment} Margin={btn.Margin} W={btn.Width} Vis={btn.IsVisible}";
                    Console.WriteLine( $"[Inspect]   {d.GetType().Name}#{name} Bounds={d.Bounds}{text}" );
                }
            }
            Console.WriteLine( $"[Inspect] ===== {cbCount} ClearableComboBox(es) =====" );

            // Open the first ClearableComboBox dropdown and inspect a ComboBoxItem
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v.GetType().Name != "ClearableComboBox" ) continue;
                var cb = (Avalonia.Controls.ComboBox) v;
                cb.IsDropDownOpen = true;
                break;
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Console.WriteLine( "[Inspect] ===== ComboBoxItem dump =====" );
            int iCount = 0;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is not Avalonia.Controls.ComboBoxItem cbi ) continue;
                iCount++;
                if ( iCount > 2 ) break;
                Console.WriteLine( $"[Inspect] -- ComboBoxItem #{iCount} Selected={cbi.IsSelected} Classes=[{string.Join( ",", cbi.Classes )}] Bounds={cbi.Bounds} --" );
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( v ) )
                {
                    var c = d as Avalonia.Controls.Control;
                    string name = c?.Name ?? "";
                    string extra = "";
                    if ( d is Avalonia.Controls.Border b ) extra = $" Bg={b.Background} BB={b.BorderBrush} BT={b.BorderThickness}";
                    if ( d is Avalonia.Controls.Presenters.ContentPresenter cp ) extra = $" Bg={cp.Background} BB={cp.BorderBrush} BT={cp.BorderThickness}";
                    Console.WriteLine( $"[Inspect]   {d.GetType().Name}#{name} Bounds={d.Bounds}{extra}" );
                }
            }
            Console.WriteLine( $"[Inspect] ===== {iCount} ComboBoxItem(s) =====" );
        }

        static void InspectMacrosHover( Avalonia.Controls.Window window )
        {
            // Switch to Macros tab
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabControl tc && tc.Items.Count > 1 )
                {
                    for ( int i = 0; i < tc.Items.Count; i++ )
                        if ( tc.Items[i] is Avalonia.Controls.TabItem ti && ti.Header?.ToString() == "Macros" )
                            { tc.SelectedIndex = i; break; }
                    break;
                }
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();

            Avalonia.Controls.TreeViewItem leaf = null;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TreeViewItem tvi && tvi.ItemCount == 0 && tvi.Bounds.Width > 0 && !tvi.IsSelected )
                { leaf = tvi; break; }
            }
            if ( leaf == null ) { Console.WriteLine( "[Inspect] No leaf TreeViewItem found" ); return; }

            void Dump( string label )
            {
                Console.WriteLine( $"[Inspect] === {label} ===" );
                Console.WriteLine( $"[Inspect] TVI Classes=[{string.Join(",", leaf.Classes)}] Bounds={leaf.Bounds} Padding={leaf.Padding} BT={leaf.BorderThickness}" );
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( leaf ) )
                {
                    var c = d as Avalonia.Controls.Control;
                    string name = c?.Name ?? "";
                    string extra = "";
                    if ( d is Avalonia.Controls.Border br ) extra = $" Bg={br.Background} BB={br.BorderBrush} BT={br.BorderThickness} Margin={br.Margin} Padding={br.Padding}";
                    if ( d is Avalonia.Controls.TextBlock tb ) extra = $" Text='{tb.Text}' Margin={tb.Margin} Padding={tb.Padding}";
                    if ( d is Avalonia.Controls.Presenters.ContentPresenter cp ) extra = $" Bg={cp.Background} BB={cp.BorderBrush} BT={cp.BorderThickness} Margin={cp.Margin} Padding={cp.Padding}";
                    if ( d is Avalonia.Controls.StackPanel sp ) extra = $" Margin={sp.Margin}";
                    if ( d is Avalonia.Controls.Grid gr ) extra = $" Margin={gr.Margin}";
                    Console.WriteLine( $"[Inspect]   {d.GetType().Name}#{name} Bounds={d.Bounds}{extra}" );
                }
            }

            Dump( "IDLE" );
            // Apply :pointerover pseudo-class
            ((Avalonia.Controls.IPseudoClasses) leaf.Classes).Add( ":pointerover" );
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Dump( "HOVER" );
        }

        static void InspectAbout( Avalonia.Controls.Window window )
        {
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabControl tc && tc.Items.Count > 1 )
                {
                    for ( int i = 0; i < tc.Items.Count; i++ )
                        if ( tc.Items[i] is Avalonia.Controls.TabItem ti && ti.Header?.ToString()?.Contains( "About" ) == true )
                            { tc.SelectedIndex = i; break; }
                    break;
                }
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Console.WriteLine( "[Inspect] ===== About PayPal button dump =====" );
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v.GetType().Name != "ImageButton" ) continue;
                var btn = v as Avalonia.Controls.Button;
                Console.WriteLine( $"[Inspect] -- ImageButton Classes=[{string.Join( ",", btn.Classes )}] Bounds={btn.Bounds} BorderBrush={btn.BorderBrush} BorderThickness={btn.BorderThickness} Background={btn.Background} --" );
                Console.WriteLine( $"[Inspect]   Template={btn.Template?.GetType().Name ?? "<null>"}" );
                int depth = 0;
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( v ) )
                {
                    depth++;
                    if ( depth > 30 ) break;
                    var c = d as Avalonia.Controls.Control;
                    string name = c?.Name ?? "";
                    string extra = "";
                    if ( d is Avalonia.Controls.Border br ) extra = $" Bg={br.Background} BB={br.BorderBrush} BT={br.BorderThickness}";
                    if ( d is Avalonia.Controls.Presenters.ContentPresenter cp ) extra = $" Bg={cp.Background} BB={cp.BorderBrush} BT={cp.BorderThickness} Content={cp.Content?.GetType().Name ?? "<null>"}";
                    Console.WriteLine( $"[Inspect]   {d.GetType().Name}#{name} Bounds={d.Bounds}{extra}" );
                }
                break;
            }
        }

        static void InspectScreenshotHover( Avalonia.Controls.Window window )
        {
            // Print all tab control headers so we can navigate reliably.
            int tcIdx = 0;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabControl tc )
                {
                    var headers = new System.Collections.Generic.List<string>();
                    for ( int i = 0; i < tc.ItemCount; i++ )
                    {
                        if ( tc.Items[i] is Avalonia.Controls.TabItem ti )
                            headers.Add( ti.Header?.ToString() ?? "<null>" );
                        else
                            headers.Add( tc.Items[i]?.ToString() ?? "<obj>" );
                    }
                    Console.WriteLine( $"[Inspect] TabControl #{tcIdx++} headers: [{string.Join(" | ", headers)}]" );
                }
            }

            // Try navigating any TabControl that has a "Screenshot" header.
            void SelectTab( string headerText )
            {
                foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
                {
                    if ( v is Avalonia.Controls.TabControl tc )
                    {
                        for ( int i = 0; i < tc.ItemCount; i++ )
                        {
                            string h = ( tc.Items[i] as Avalonia.Controls.TabItem )?.Header?.ToString() ?? tc.Items[i]?.ToString();
                            if ( h != null && h.Contains( headerText, StringComparison.OrdinalIgnoreCase ) )
                            {
                                tc.SelectedIndex = i;
                                Console.WriteLine( $"[Inspect] Selected tab '{h}' (index {i})" );
                                return;
                            }
                        }
                    }
                }
                Console.WriteLine( $"[Inspect] No tab matching '{headerText}' found" );
            }

            SelectTab( "Agents" );
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            SelectTab( "Screenshot" );
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();

            // Find the screenshot tab's ListBox (x:Name="listBox" inside ScreenshotTabControl).
            Avalonia.Controls.ListBox listBox = null;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.ListBox lb && lb.Name == "listBox" )
                {
                    listBox = lb;
                    break;
                }
            }
            if ( listBox == null ) { Console.WriteLine( "[Inspect] Screenshot ListBox not found" ); return; }

            // Inject two dummy entries so we have ListBoxItems to inspect even
            // when no real screenshots exist on disk.
            var vmType = listBox.DataContext?.GetType();
            var screenshotsProp = vmType?.GetProperty( "Screenshots" );
            var screenshots = screenshotsProp?.GetValue( listBox.DataContext );
            if ( screenshots is System.Collections.IList list )
            {
                var entryType = vmType.GetNestedType( "ScreenshotEntry" ) ?? Type.GetType( "ClassicAssist.UI.ViewModels.Agents.ScreenshotTabViewModel+ScreenshotEntry, ClassicAssist" );
                if ( entryType != null )
                {
                    for ( int i = 0; i < 2; i++ )
                    {
                        var entry = Activator.CreateInstance( entryType );
                        entryType.GetProperty( "Path" )?.SetValue( entry, $"fake-{i}.png" );
                        entryType.GetProperty( "Extension" )?.SetValue( entry, "PNG" );
                        list.Add( entry );
                    }
                }
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();

            // Find the first ListBoxItem in this listBox specifically.
            Avalonia.Controls.ListBoxItem lbi = null;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( listBox ) )
            {
                if ( v is Avalonia.Controls.ListBoxItem item ) { lbi = item; break; }
            }
            if ( lbi == null ) { Console.WriteLine( "[Inspect] ListBoxItem not found inside screenshot listBox" ); return; }

            void Dump( string label )
            {
                Console.WriteLine( $"[Inspect] === {label} ===" );
                Console.WriteLine( $"[Inspect] LBI Classes=[{string.Join(",", lbi.Classes)}] PseudoClasses=[{string.Join(",", lbi.Classes)}] Bounds={lbi.Bounds} Padding={lbi.Padding} BT={lbi.BorderThickness} BB={lbi.BorderBrush} Bg={lbi.Background}" );
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( lbi ) )
                {
                    var c = d as Avalonia.Controls.Control;
                    string name = c?.Name ?? "";
                    string extra = "";
                    if ( d is Avalonia.Controls.Border br ) extra = $" Bg={br.Background} BB={br.BorderBrush} BT={br.BorderThickness} Margin={br.Margin} Padding={br.Padding}";
                    if ( d is Avalonia.Controls.Presenters.ContentPresenter cp ) extra = $" Bg={cp.Background} BB={cp.BorderBrush} BT={cp.BorderThickness} Margin={cp.Margin} Padding={cp.Padding}";
                    if ( d is Avalonia.Controls.Grid gr ) extra = $" Margin={gr.Margin}";
                    Console.WriteLine( $"[Inspect]   {d.GetType().Name}#{name} Bounds={d.Bounds}{extra}" );
                }
            }

            Dump( "IDLE" );
            ( (Avalonia.Controls.IPseudoClasses) lbi.Classes ).Add( ":pointerover" );
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Dump( "HOVER" );
            ( (Avalonia.Controls.IPseudoClasses) lbi.Classes ).Add( ":selected" );
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Dump( "SELECTED+HOVER" );
        }

        static void InspectVendor( Avalonia.Controls.Window window )
        {
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabControl tc && tc.Items.Count > 1 )
                {
                    for ( int i = 0; i < tc.Items.Count; i++ )
                        if ( tc.Items[i] is Avalonia.Controls.TabItem ti && ti.Header?.ToString()?.Contains( "Agents" ) == true )
                            { tc.SelectedIndex = i; break; }
                    break;
                }
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabItem ti && ti.Header?.ToString()?.Contains( "Vendor Sell" ) == true )
                { ti.IsSelected = true; break; }
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Console.WriteLine( "[Inspect] ===== VendorSell DataGridColumnHeader dump =====" );
            int hCount = 0;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v.GetType().Name != "DataGridColumnHeader" ) continue;
                hCount++;
                var cc = v as Avalonia.Controls.ContentControl;
                Console.WriteLine( $"[Inspect] -- Header #{hCount} Content='{cc?.Content}' Bounds={v.Bounds} --" );
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( v ) )
                {
                    var c = d as Avalonia.Controls.Control;
                    string name = c?.Name ?? "";
                    string extra = "";
                    if ( d is Avalonia.Controls.Shapes.Path p ) extra = $" Data='{p.Data}' Vis={p.IsVisible} W={p.Width} H={p.Height} Margin={p.Margin}";
                    if ( d is Avalonia.Controls.Shapes.Rectangle r ) extra = $" Fill={r.Fill} Vis={r.IsVisible} W={r.Width} H={r.Height}";
                    if ( d is Avalonia.Controls.Primitives.Thumb t ) extra = $" Vis={t.IsVisible} W={t.Width} HA={t.HorizontalAlignment}";
                    if ( d is Avalonia.Controls.Grid g ) extra = $" ColDef='{string.Join(",", g.ColumnDefinitions.Select(cd => cd.Width.ToString()))}'";
                    Console.WriteLine( $"[Inspect]     {d.GetType().Name}#{name} Bounds={d.Bounds}{extra}" );
                }
            }
            Console.WriteLine( $"[Inspect] ===== {hCount} headers =====" );
        }

        static void InspectOrganizer( Avalonia.Controls.Window window )
        {
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabControl tc && tc.Items.Count > 1 )
                {
                    for ( int i = 0; i < tc.Items.Count; i++ )
                        if ( tc.Items[i] is Avalonia.Controls.TabItem ti && ti.Header?.ToString()?.Contains( "Agents" ) == true )
                            { tc.SelectedIndex = i; break; }
                    break;
                }
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            // Click the Organizer sub-tab (TabControl inside Agents tab)
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabItem ti && ti.Header?.ToString()?.Contains( "Organizer" ) == true )
                { ti.IsSelected = true; break; }
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Console.WriteLine( "[Inspect] ===== Organizer scroll dump =====" );
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.Primitives.ScrollBar sb )
                {
                    var ctl = (Avalonia.Controls.Control) sb;
                    Console.WriteLine( $"[Inspect] ScrollBar Bounds={sb.Bounds} Orient={sb.Orientation} Vis={ctl.IsVisible} Classes=[{string.Join(",", ctl.Classes)}] StyleKey={sb.GetType().Name}" );
                }
                if ( v is Avalonia.Controls.ScrollViewer sv )
                {
                    Console.WriteLine( $"[Inspect] ScrollViewer Bounds={sv.Bounds} HSV={sv.HorizontalScrollBarVisibility} VSV={sv.VerticalScrollBarVisibility} Extent={sv.Extent} Viewport={sv.Viewport}" );
                }
                if ( v is Avalonia.Controls.DataGrid dg )
                {
                    Console.WriteLine( $"[Inspect] DataGrid Bounds={dg.Bounds} Width={dg.Width} HA={dg.HorizontalAlignment}" );
                }
            }
        }

        static void InspectDebugPackets( Avalonia.Controls.Window mainWindow )
        {
            Console.WriteLine( "[Inspect] ===== Debug → Packets ComboBox dump =====" );

            // Open DebugWindow as if user clicked the Debug button.
            var debugWindow = new ClassicAssist.UI.Views.DebugWindow();
            debugWindow.Show( mainWindow );
            ( debugWindow as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( debugWindow as Avalonia.Layout.Layoutable )?.UpdateLayout();

            // DebugWindow's first tab is Packets so DebugPacketsControl should already be realized.
            // Find the "Enabled Packets" ComboBox: it's the one with a custom template containing ToggleGrid.
            Avalonia.Controls.ComboBox targetCombo = null;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( debugWindow ) )
            {
                if ( v is not Avalonia.Controls.ComboBox cb ) continue;
                // The Enabled Packets combo binds to PacketEntries; the Direction combo binds to Direction enum.
                // Distinguish by Width — Enabled Packets has Width=100, Direction has MinWidth=100.
                if ( cb.Width == 100 ) { targetCombo = cb; break; }
            }

            if ( targetCombo == null )
            {
                Console.WriteLine( "[Inspect] FAIL: could not find Enabled Packets ComboBox." );
                debugWindow.Close();
                return;
            }

            Console.WriteLine( $"[Inspect] Found ComboBox Bounds={targetCombo.Bounds} ItemsSource Count={(targetCombo.ItemsSource as System.Collections.IList)?.Count ?? -1}" );
            Console.WriteLine( $"[Inspect] ItemContainerTheme assigned: {targetCombo.ItemContainerTheme != null}" );
            if ( targetCombo.ItemContainerTheme != null )
            {
                var ict = targetCombo.ItemContainerTheme;
                Console.WriteLine( $"[Inspect]   ICT TargetType={ict.TargetType?.Name} BasedOn={ict.BasedOn != null} ChildStyles.Count={ict.Children.Count}" );
                foreach ( var child in ict.Children )
                {
                    var s = child as Avalonia.Styling.Style;
                    if ( s != null ) Console.WriteLine( $"[Inspect]   ICT child Style Selector={s.Selector} SetterCount={s.Setters.Count}" );
                }
            }

            // Force open the dropdown so items realize.
            targetCombo.IsDropDownOpen = true;
            ( debugWindow as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( debugWindow as Avalonia.Layout.Layoutable )?.UpdateLayout();
            // Give the popup an extra layout pass.
            for ( int i = 0; i < 3; i++ )
            {
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                ( debugWindow as Avalonia.Layout.Layoutable )?.UpdateLayout();
            }
            Console.WriteLine( $"[Inspect] After open: IsDropDownOpen={targetCombo.IsDropDownOpen}" );

            // Resolve via ContainerFromItem — popups host items in OverlayLayer/PopupRoot
            // which isn't reachable from GetVisualDescendants(window).
            int itemCount = 0;
            int hoverIdx = -1;
            var allItems = new System.Collections.Generic.List<Avalonia.Controls.ComboBoxItem>();
            var src = targetCombo.ItemsSource as System.Collections.IList;
            if ( src != null )
            {
                for ( int i = 0; i < Math.Min( src.Count, 10 ); i++ )
                {
                    var c = targetCombo.ContainerFromItem( src[i] );
                    if ( c is Avalonia.Controls.ComboBoxItem cbi ) allItems.Add( cbi );
                }
            }
            Console.WriteLine( $"[Inspect] Found {allItems.Count} ComboBoxItems via ContainerFromItem." );
            if ( allItems.Count == 0 )
            {
                // Items haven't materialized — try ContainerFromIndex too.
                for ( int i = 0; i < 5; i++ )
                {
                    var c = targetCombo.ContainerFromIndex( i );
                    if ( c is Avalonia.Controls.ComboBoxItem cbi ) allItems.Add( cbi );
                }
                Console.WriteLine( $"[Inspect] After ContainerFromIndex: {allItems.Count}" );
            }
            // Also dump the popup's tree if we can find it.
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( targetCombo ) )
            {
                if ( v is Avalonia.Controls.Primitives.Popup pop )
                {
                    Console.WriteLine( $"[Inspect] Popup IsOpen={pop.IsOpen} Placement={pop.Placement} HasChild={pop.Child != null} ChildType={pop.Child?.GetType().Name}" );
                    if ( pop.Child != null )
                    {
                        foreach ( var pd in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( pop.Child ) )
                        {
                            if ( pd is Avalonia.Controls.ComboBoxItem cbi2 ) allItems.Add( cbi2 );
                            if ( pd is Avalonia.Controls.Presenters.ItemsPresenter ip )
                                Console.WriteLine( $"[Inspect]   ItemsPresenter Bounds={ip.Bounds} Panel={ip.Panel?.GetType().Name} Children={ip.Panel?.Children.Count}" );
                        }
                    }
                }
            }
            Console.WriteLine( $"[Inspect] After popup walk: {allItems.Count} items collected." );

            // Simulate :pointerover on the first item by setting the pseudoclass directly.
            if ( allItems.Count > 0 )
            {
                hoverIdx = 0;
                ( (Avalonia.Controls.IPseudoClasses) allItems[hoverIdx].Classes ).Set( ":pointerover", true );
                ( debugWindow as Avalonia.Layout.Layoutable )?.UpdateLayout();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            }

            for ( int i = 0; i < allItems.Count && itemCount < 5; i++ )
            {
                var cbi = allItems[i];
                itemCount++;
                string state = i == hoverIdx ? "[HOVER]" : "[idle]";
                Console.WriteLine( $"[Inspect] -- ComboBoxItem #{i} {state} Bounds={cbi.Bounds} --" );
                Console.WriteLine( $"[Inspect]   Foreground={cbi.Foreground} Background={cbi.Background} BorderBrush={cbi.BorderBrush}" );
                Console.WriteLine( $"[Inspect]   Classes=[{string.Join( ",", cbi.Classes )}] Pseudos=[{string.Join( ",", cbi.Classes.Where( c => c.StartsWith( ":" ) ) )}]" );
                // Dive into template parts to see what PART_ContentPresenter actually has.
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( cbi ) )
                {
                    var ctl = d as Avalonia.Controls.Control;
                    string name = ctl?.Name ?? "";
                    if ( d is Avalonia.Controls.Presenters.ContentPresenter cp )
                    {
                        Console.WriteLine( $"[Inspect]     ContentPresenter#{name} Bg={cp.Background} BB={cp.BorderBrush} BT={cp.BorderThickness} FG={cp.Foreground}" );
                    }
                    else if ( d is Avalonia.Controls.CheckBox chk )
                    {
                        Console.WriteLine( $"[Inspect]     CheckBox Content='{chk.Content}' FG={chk.Foreground} Bg={chk.Background}" );
                    }
                    else if ( d is Avalonia.Controls.TextBlock tb && !string.IsNullOrWhiteSpace( tb.Text ) )
                    {
                        Console.WriteLine( $"[Inspect]     TextBlock '{tb.Text}' FG={tb.Foreground}" );
                    }
                }
            }

            Console.WriteLine( "[Inspect] ===== End Debug → Packets dump =====" );

            // Close the debug window so the smoke loop / interactive mode doesn't keep it hanging.
            try { targetCombo.IsDropDownOpen = false; } catch { }
            try { debugWindow.Close(); } catch { }
        }

        static void InspectSkills( Avalonia.Controls.Window window )
        {
            // Switch to Skills tab
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabControl tc && tc.Items.Count > 1 )
                {
                    for ( int i = 0; i < tc.Items.Count; i++ )
                        if ( tc.Items[i] is Avalonia.Controls.TabItem ti && ti.Header?.ToString()?.Contains( "Skills" ) == true )
                            { tc.SelectedIndex = i; break; }
                    break;
                }
            }
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();

            Console.WriteLine( "[Inspect] ===== Skills tab dump =====" );
            int dgCount = 0;
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is not Avalonia.Controls.DataGrid dg ) continue;
                if ( dg.Bounds.Width <= 0 ) continue;
                dgCount++;
                Console.WriteLine( $"[Inspect] -- DataGrid #{dgCount} Bounds={dg.Bounds} BT={dg.BorderThickness} BB={dg.BorderBrush} --" );
                int depth = 0;
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( dg ) )
                {
                    depth++;
                    if ( depth > 800 ) break;
                    var c = d as Avalonia.Controls.Control;
                    string name = c?.Name ?? "";
                    string extra = "";
                    if ( d is Avalonia.Controls.Shapes.Rectangle rect ) extra = $" Fill={rect.Fill} H={rect.Height} W={rect.Width} IsVisible={rect.IsVisible}";
                    if ( d is Avalonia.Controls.Border br ) extra = $" Bg={br.Background} BB={br.BorderBrush} BT={br.BorderThickness}";
                    if ( d.GetType().Name == "DataGridColumnHeader" )
                    {
                        var tc = d as Avalonia.Controls.Primitives.TemplatedControl;
                        var cc = d as Avalonia.Controls.ContentControl;
                        extra = $" Content='{cc?.Content}' BB={tc?.BorderBrush} BT={tc?.BorderThickness}";
                    }
                    Console.WriteLine( $"[Inspect]   {d.GetType().Name}#{name} Bounds={d.Bounds}{extra}" );
                }
                break;
            }
            Console.WriteLine( $"[Inspect] ===== {dgCount} DataGrid(s) inspected =====" );
        }

        static void InspectTreeView( Avalonia.Controls.Window window )
        {
            // Switch to Hotkeys tab so the TreeView gets realized.
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is Avalonia.Controls.TabControl tc && tc.Items.Count > 1 )
                {
                    for ( int i = 0; i < tc.Items.Count; i++ )
                    {
                        if ( tc.Items[i] is Avalonia.Controls.TabItem ti && ti.Header?.ToString()?.Contains( "Hotkeys" ) == true )
                        {
                            tc.SelectedIndex = i;
                            Console.WriteLine( $"[Inspect] Selected Hotkeys tab at index {i}" );
                            break;
                        }
                    }
                    break;
                }
            }
            // Layout pass.
            ( window as Avalonia.Layout.Layoutable )?.UpdateLayout();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Console.WriteLine( "[Inspect] ===== TreeView dump =====" );
            foreach ( var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( window ) )
            {
                if ( v is not Avalonia.Controls.TreeView tv ) continue;
                if ( tv.Bounds.Width <= 0 ) continue;
                Console.WriteLine( $"[Inspect] -- TreeView Bounds={tv.Bounds} --" );
                int tviCount = 0;
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( tv ) )
                {
                    if ( d is not Avalonia.Controls.TreeViewItem tvi ) continue;
                    tviCount++;
                    if ( tviCount > 2 ) break;
                    Console.WriteLine( $"[Inspect]   TVI Header='{tvi.Header}' Bounds={tvi.Bounds} Padding={tvi.Padding} BT={tvi.BorderThickness} BB={tvi.BorderBrush}" );
                    foreach ( var dd in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( tvi ) )
                    {
                        var ctl = dd as Avalonia.Controls.Control;
                        string name = ctl?.Name ?? "";
                        string extra = "";
                        if ( dd is Avalonia.Controls.Shapes.Path p ) extra = $" Data='{p.Data}' Fill={p.Fill} Stroke={p.Stroke} W={p.Width} H={p.Height} RT={p.RenderTransform}";
                        if ( dd is Avalonia.Controls.Border b ) extra = $" Bg={b.Background} BB={b.BorderBrush} BT={b.BorderThickness}";
                        if ( dd is Avalonia.Controls.Primitives.ToggleButton tb ) extra += $" IsChecked={tb.IsChecked} Foreground={tb.Foreground} Background={tb.Background}";
                        Console.WriteLine( $"[Inspect]     {dd.GetType().Name}#{name} Bounds={dd.Bounds}{extra}" );
                    }
                }
                // Find the vertical scrollbar and dump everything inside.
                foreach ( var d in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( tv ) )
                {
                    if ( d is Avalonia.Controls.Primitives.ScrollBar sb &&
                         ( (Avalonia.Controls.Control) d ).Classes.Contains( ":vertical" ) )
                    {
                        Console.WriteLine( $"[Inspect]   ScrollBar Bounds={sb.Bounds}" );
                        foreach ( var sbd in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants( sb ) )
                        {
                            var sbc = sbd as Avalonia.Controls.Control;
                            string sbname = sbc?.Name ?? "";
                            string sbclasses = sbc != null && sbc.Classes.Count > 0 ? $" Classes=[{string.Join( ",", sbc.Classes )}]" : "";
                            string extra = "";
                            if ( sbd is Avalonia.Controls.Shapes.Path p )
                                extra = $" Fill={p.Fill}";
                            if ( sbd is Avalonia.Controls.Border b )
                                extra = $" Bg={b.Background}";
                            if ( sbd is Avalonia.Controls.Primitives.TemplatedControl tcc )
                                extra += $" Foreground={tcc.Foreground} Background={tcc.Background}";
                            Console.WriteLine( $"[Inspect]   {sbd.GetType().Name}#{sbname} Bounds={sbd.Bounds}{sbclasses}{extra}" );
                        }
                        break;
                    }
                }
                break;
            }
            Console.WriteLine( "[Inspect] ===== done =====" );
        }
    }
}
