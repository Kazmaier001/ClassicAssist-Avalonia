using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ClassicAssist.UO.Data;

namespace ClassicAssist.UI.Misc.ValueConverters
{
    public class LockStatusValueConverter : IValueConverter
    {
        // .NET 10 removed BinaryFormatter, which is what System.Resources used
        // to deserialise the System.Drawing.Bitmap entries inside
        // Properties.Resources.resources. Reading Properties.Resources.arrow_up
        // therefore throws and the converter previously returned a null Image
        // source, so the Status column rendered blank.
        //
        // Bypass that path entirely: the same PNGs are also registered as
        // AvaloniaResource in ClassicAssist.csproj and load via avares:// URIs.
        private static readonly IImage UpImage = LoadAsset( "avares://ClassicAssist/Resources/arrow_up.png" );
        private static readonly IImage DownImage = LoadAsset( "avares://ClassicAssist/Resources/arrow_down.png" );
        private static readonly IImage LockedImage = LoadAsset( "avares://ClassicAssist/Resources/lock.png" );

        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( !( value is LockStatus lockStatus ) )
            {
                return value;
            }

            switch ( lockStatus )
            {
                case LockStatus.Up: return UpImage;
                case LockStatus.Down: return DownImage;
                case LockStatus.Locked: return LockedImage;
                default: return null;
            }
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return BindingOperations.DoNothing;
        }

        private static IImage LoadAsset( string uri )
        {
            try
            {
                return new Bitmap( AssetLoader.Open( new Uri( uri ) ) );
            }
            catch
            {
                return null;
            }
        }
    }
}
