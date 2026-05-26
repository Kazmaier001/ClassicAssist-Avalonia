using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ClassicAssist.Misc;
using ClassicAssist.Shared.UI;
using ClassicAssist.UO.Data;
using Microsoft.Scripting.Utils;

namespace ClassicAssist.UI.Views.Filters.ItemIDFilter
{
    /// <summary>
    ///     Interaction logic for ItemIDSelectionWindow.xaml
    /// </summary>
    public partial class ItemIDSelectionWindow : Window, INotifyPropertyChanged
    {
        private bool _imagesLoaded;
        private ICommand _okCommand;
        private string _searchText;
        private ImageData _selectedItem;
        private DispatcherTimer _spinnerTimer;

        public ItemIDSelectionWindow()
        {
            InitializeComponent();

            StartSpinner();
            LoadImageData().ConfigureAwait( false );
        }

        private void StartSpinner()
        {
            var image = this.FindControl<Image>( "LoadSpinner" );
            if ( image?.RenderTransform is not RotateTransform rotate )
            {
                return;
            }

            _spinnerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds( 33 ) };
            _spinnerTimer.Tick += ( _, _ ) =>
            {
                if ( ImagesLoaded )
                {
                    _spinnerTimer.Stop();
                    return;
                }

                rotate.Angle = ( rotate.Angle - 6 ) % 360;
            };
            _spinnerTimer.Start();
        }

        public ObservableCollection<ImageData> FilterImages { get; set; } = new ObservableCollection<ImageData>();
        public ObservableCollection<ImageData> Images { get; set; } = new ObservableCollection<ImageData>();

        public bool ImagesLoaded
        {
            get => _imagesLoaded;
            set => SetField( ref _imagesLoaded, value );
        }

        public ICommand OKCommand => _okCommand ?? ( _okCommand = new RelayCommand( OK, o => o != null ) );

        public bool Result { get; set; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetField( ref _searchText, value );
                UpdateFilter();
            }
        }

        public ImageData SelectedItem
        {
            get => _selectedItem;
            set => SetField( ref _selectedItem, value );
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private Task LoadImageData()
        {
            return Task.Run( async () =>
            {
                List<ImageData> images = new List<ImageData>();

                for ( int i = 0; i < 10000; i++ )
                {
                    IImage image = Art.GetStatic( i, 0 ).ToImageSource();

                    images.Add( new ImageData
                    {
                        ID = i, IImage = image, Name = TileData.GetStaticTile( i ).Name
                    } );
                }

                // Push to both Images (source) and FilterImages (bound) in small
                // batches at Background priority. FilterImages is what the
                // ListBox actually realizes — adding 10k items in one shot locks
                // the UI thread for seconds and freezes the spinner. Batches
                // let the DispatcherTimer (Normal priority) preempt and tick.
                const int batchSize = 100;
                for ( int i = 0; i < images.Count; i += batchSize )
                {
                    int start = i;
                    int count = Math.Min( batchSize, images.Count - start );

                    await Dispatcher.UIThread.InvokeAsync( () =>
                    {
                        for ( int j = 0; j < count; j++ )
                        {
                            ImageData entry = images[start + j];
                            Images.Add( entry );
                            // Mirror initial unfiltered population into the
                            // bound collection so the ListBox realizes
                            // progressively rather than all at once.
                            if ( string.IsNullOrEmpty( SearchText ) )
                            {
                                FilterImages.Add( entry );
                            }
                        }
                    }, DispatcherPriority.Background );
                }

                await Dispatcher.UIThread.InvokeAsync( () =>
                {
                    // If a filter was typed during load, recompute now.
                    if ( !string.IsNullOrEmpty( SearchText ) )
                    {
                        UpdateFilter();
                    }
                }, DispatcherPriority.Background );

                // Final paint settle: wait for a render pass, give Skia a
                // few frames, then flip ImagesLoaded at ApplicationIdle so
                // the spinner hides only once the list is visually settled.
                await Dispatcher.UIThread.InvokeAsync( () => { }, DispatcherPriority.Render );
                await Task.Delay( 200 );
                await Dispatcher.UIThread.InvokeAsync( () => ImagesLoaded = true,
                    DispatcherPriority.ApplicationIdle );
            } );
        }

        private void UpdateFilter()
        {
            if ( string.IsNullOrEmpty( SearchText ) )
            {
                FilterImages.AddRange( Images );
                return;
            }

            List<ImageData> filterItems = Images.Where( e => e.Name.Contains( SearchText ) ).ToList();

            if ( int.TryParse( SearchText, out int searchInt ) )
            {
                filterItems.AddRange( Images.Where( e => e.ID.Equals( searchInt ) ) );
            }

            if ( SearchText.StartsWith( "0x" ) && SearchText.Length >= 3 )
            {
                int int32 = Convert.ToInt32( SearchText, 16 );

                filterItems.AddRange( Images.Where( e => e.ID.Equals( int32 ) ) );
            }

            FilterImages.Clear();
            FilterImages.AddRange( filterItems );
        }

        protected virtual void OnPropertyChanged( [CallerMemberName] string propertyName = null )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

        protected bool SetField<T>( ref T field, T value, [CallerMemberName] string propertyName = null )
        {
            if ( EqualityComparer<T>.Default.Equals( field, value ) )
            {
                return false;
            }

            field = value;
            OnPropertyChanged( propertyName );
            return true;
        }

        private void OK( object obj )
        {
            // Set Result BEFORE Close so the awaiting caller (ShowDialogAsync)
            // observes Result=true. In Avalonia, Button's Click event fires
            // BEFORE the bound Command — so CloseOnClickBehaviour on the OK
            // button would close the window first, leaving Result=false. We
            // therefore drop the behaviour from the OK button and let this
            // method handle both side effects atomically.
            Result = true;
            Close();
        }

        public class ImageData
        {
            public int ID { get; set; }
            public IImage IImage { get; set; }
            public string Name { get; set; }
        }
    }
}