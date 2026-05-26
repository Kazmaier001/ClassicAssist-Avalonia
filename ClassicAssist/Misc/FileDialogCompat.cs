using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace ClassicAssist.Misc
{
    /// <summary>
    /// Compatibility shim for WPF file dialogs -> Avalonia StorageProvider
    /// </summary>
    internal static class FileDialogPump
    {
        // Sync-over-async: callers expect WPF-style bool? ShowDialog() but
        // StorageProvider only exposes async. task.Wait() deadlocks the UI thread
        // (awaiter continuation needs the same thread we just blocked). Mirror
        // MessageBoxCompat: pump a nested dispatcher loop until the task completes.
        public static T RunSync<T>( Func<Task<T>> factory )
        {
            if ( !Dispatcher.UIThread.CheckAccess() )
            {
                T workerResult = default;
                using ( ManualResetEventSlim mre = new ManualResetEventSlim() )
                {
                    Dispatcher.UIThread.Post( async () =>
                    {
                        try { workerResult = await factory(); }
                        finally { mre.Set(); }
                    } );
                    mre.Wait();
                }
                return workerResult;
            }

            Task<T> task = factory();
            CancellationTokenSource cts = new CancellationTokenSource();
            task.ContinueWith( _ => cts.Cancel() );
            Dispatcher.UIThread.MainLoop( cts.Token );
            return task.IsCompletedSuccessfully ? task.Result : default;
        }
    }

    public class OpenFileDialog
    {
        public string Filter { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public bool Multiselect { get; set; }
        public string InitialDirectory { get; set; }

        public bool? ShowDialog()
        {
            return FileDialogPump.RunSync( ShowDialogAsync );
        }

        public async Task<bool?> ShowDialogAsync()
        {
            var window = GetMainWindow();
            if ( window == null ) return false;

            var filters = ParseFilter( Filter );
            var result = await window.StorageProvider.OpenFilePickerAsync( new FilePickerOpenOptions
            {
                Title = Title,
                AllowMultiple = Multiselect,
                FileTypeFilter = filters
            } );

            if ( result != null && result.Count > 0 )
            {
                FileName = result[0].Path.LocalPath;
                return true;
            }
            return false;
        }

        private static List<FilePickerFileType> ParseFilter( string filter )
        {
            var types = new List<FilePickerFileType>();
            if ( string.IsNullOrEmpty( filter ) ) return types;
            var parts = filter.Split( '|' );
            for ( int i = 0; i < parts.Length - 1; i += 2 )
            {
                var patterns = parts[i + 1].Split( ';' );
                types.Add( new FilePickerFileType( parts[i] ) { Patterns = new List<string>( patterns ) } );
            }
            return types;
        }

        private static Window GetMainWindow()
        {
            if ( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
                return desktop.MainWindow;
            return null;
        }
    }

    public class SaveFileDialog
    {
        public string Filter { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public bool OverwritePrompt { get; set; } = true;
        public string DefaultExt { get; set; }
        public string InitialDirectory { get; set; }

        public bool? ShowDialog()
        {
            return FileDialogPump.RunSync( ShowDialogAsync );
        }

        public async Task<bool?> ShowDialogAsync()
        {
            var window = GetMainWindow();
            if ( window == null ) return false;

            var filters = ParseFilter( Filter );
            var result = await window.StorageProvider.SaveFilePickerAsync( new FilePickerSaveOptions
            {
                Title = Title,
                FileTypeChoices = filters,
                SuggestedFileName = Path.GetFileName( FileName ),
                DefaultExtension = DefaultExt
            } );

            if ( result != null )
            {
                FileName = result.Path.LocalPath;
                return true;
            }
            return false;
        }

        private static List<FilePickerFileType> ParseFilter( string filter )
        {
            var types = new List<FilePickerFileType>();
            if ( string.IsNullOrEmpty( filter ) ) return types;
            var parts = filter.Split( '|' );
            for ( int i = 0; i < parts.Length - 1; i += 2 )
            {
                var patterns = parts[i + 1].Split( ';' );
                types.Add( new FilePickerFileType( parts[i] ) { Patterns = new List<string>( patterns ) } );
            }
            return types;
        }

        private static Window GetMainWindow()
        {
            if ( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
                return desktop.MainWindow;
            return null;
        }
    }

    public class FolderBrowserDialog
    {
        public string Description { get; set; }
        public string SelectedPath { get; set; }
        public bool ShowNewFolderButton { get; set; }

        public bool? ShowDialog()
        {
            return FileDialogPump.RunSync( ShowDialogAsync );
        }

        public async Task<bool?> ShowDialogAsync()
        {
            var window = GetMainWindow();
            if ( window == null ) return false;

            var result = await window.StorageProvider.OpenFolderPickerAsync( new FolderPickerOpenOptions
            {
                Title = Description,
                AllowMultiple = false
            } );

            if ( result != null && result.Count > 0 )
            {
                SelectedPath = result[0].Path.LocalPath;
                return true;
            }
            return false;
        }

        private static Window GetMainWindow()
        {
            if ( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
                return desktop.MainWindow;
            return null;
        }
    }
}