using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using ClassicAssist.Annotations;
using ClassicAssist.Data.Filters;

namespace ClassicAssist.UI.Views.Filters
{
    public class SoundFilterCategory
    {
        public string Name { get; set; }
        public bool IsExpanded { get; set; } = true;
        public List<SoundFilterEntry> Items { get; set; }
    }

    /// <summary>
    ///     Interaction logic for SoundFilterConfigureWindow.xaml
    /// </summary>
    public partial class SoundFilterConfigureWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<SoundFilterEntry> _items;
        private List<SoundFilterCategory> _categories;

        public SoundFilterConfigureWindow()
        {
            InitializeComponent();
        }

        public SoundFilterConfigureWindow( ObservableCollection<SoundFilterEntry> items )
        {
            InitializeComponent();
            Items = items;
        }

        public ObservableCollection<SoundFilterEntry> Items
        {
            get => _items;
            set
            {
                _items = value;
                Categories = value?
                    .GroupBy( i => i.Category ?? string.Empty )
                    .OrderBy( g => g.Key )
                    .Select( g => new SoundFilterCategory
                    {
                        Name = g.Key,
                        Items = g.ToList()
                    } )
                    .ToList();
                OnPropertyChanged();
            }
        }

        public List<SoundFilterCategory> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged( [CallerMemberName] string propertyName = null )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }
    }
}
