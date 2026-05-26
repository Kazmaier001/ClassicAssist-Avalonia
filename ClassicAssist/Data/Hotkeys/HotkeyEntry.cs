using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ClassicAssist.Misc;
using ClassicAssist.Shared.UI;
using Newtonsoft.Json;

namespace ClassicAssist.Data.Hotkeys
{
    public abstract class HotkeyEntry : SetPropertyNotifyChanged, IComparable<HotkeyEntry>
    {
        public delegate void HotkeyChangedEventHandler( object sender, HotkeyChangedEventArgs e );

        private readonly object _childrenLock = new object();

        private bool _canGlobal = true;

        private ObservableCollectionEx<HotkeyEntry> _children = new ObservableCollectionEx<HotkeyEntry>();

        private ShortcutKeys _hotkey = new ShortcutKeys();
        private bool _isCategory;

        private bool _isGlobal;

        private string _name;
        private bool _passToUo = true;

        [JsonIgnore]
        public Action<HotkeyEntry, object[]> Action { get; set; }

        public bool CanGlobal
        {
            get => _canGlobal;
            set => SetProperty( ref _canGlobal, value );
        }

        [JsonIgnore]
        public ObservableCollectionEx<HotkeyEntry> Children
        {
            get
            {
                lock ( _childrenLock )
                {
                    return _children;
                }
            }
            set
            {
                lock ( _childrenLock )
                {
                    SetProperty( ref _children, value );
                }
            }
        }

        public virtual bool Configurable { get; set; } = false;

        public virtual bool Disableable { get; set; } = true;

        public ShortcutKeys Hotkey
        {
            get => _hotkey;
            set
            {
                if ( !Equals( value, ShortcutKeys.Default ) )
                {
                    HotkeyManager manager = HotkeyManager.GetInstance();
                    manager.ClearPreviousHotkey( value );
                }

                SetProperty( ref _hotkey, value );
                OnPropertyChanged( nameof( Image ) );
                HotkeyChanged?.Invoke( this, new HotkeyChangedEventArgs( _hotkey, value ) );
            }
        }

        private static readonly Lazy<Bitmap> _redCircle = new Lazy<Bitmap>( () =>
            new Bitmap( AssetLoader.Open( new Uri( "avares://ClassicAssist/Resources/red-circle.png" ) ) ) );

        private static readonly Lazy<Bitmap> _greenCircle = new Lazy<Bitmap>( () =>
            new Bitmap( AssetLoader.Open( new Uri( "avares://ClassicAssist/Resources/green-circle.png" ) ) ) );

        [JsonIgnore]
        public IImage Image =>
            IsCategory ? null :
            Equals( Hotkey, ShortcutKeys.Default ) ? _redCircle.Value : _greenCircle.Value;

        public bool IsCategory
        {
            get => _isCategory;
            set => SetProperty( ref _isCategory, value );
        }

        public bool IsGlobal
        {
            get => _isGlobal;
            set => SetProperty( ref _isGlobal, value );
        }

        public virtual string Name
        {
            get => _name;
            set => SetProperty( ref _name, value );
        }

        public bool PassToUO
        {
            get => _passToUo;
            set => SetProperty( ref _passToUo, value );
        }

        public int CompareTo( HotkeyEntry other )
        {
            if ( ReferenceEquals( this, other ) )
            {
                return 0;
            }

            if ( ReferenceEquals( null, other ) )
            {
                return 1;
            }

            int isCategoryComparison = _isCategory.CompareTo( other._isCategory );

            if ( isCategoryComparison != 0 )
            {
                return isCategoryComparison;
            }

            return string.Compare( _name, other._name, StringComparison.Ordinal );
        }

        public event HotkeyChangedEventHandler HotkeyChanged;

        public override string ToString()
        {
            return Name;
        }
    }
}