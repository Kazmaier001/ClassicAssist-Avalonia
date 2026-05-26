using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace ClassicAssist.Misc
{
    public static class ExtensionMethods
    {
        public static T ReadStruct<T>( this Stream stream ) where T : struct
        {
            int size = Marshal.SizeOf( typeof( T ) );

            byte[] buffer = new byte[size];

            stream.Read( buffer, 0, size );

            GCHandle pinnedBuffer = GCHandle.Alloc( buffer, GCHandleType.Pinned );

            T structure = (T) Marshal.PtrToStructure( pinnedBuffer.AddrOfPinnedObject(), typeof( T ) );

            pinnedBuffer.Free();

            return structure;
        }

        public static T GetPropertyAttribute<T>( this Type type, string propertyName )
        {
            if ( type == null )
            {
                return default;
            }

            T attr = default;

            PropertyInfo pi = type.GetProperty( propertyName );

            if ( pi != null )
            {
                attr = pi.GetCustomAttributes( false ).OfType<T>().SingleOrDefault();
            }

            return attr != null ? attr : default;
        }

        // PNG round-trip from SkiaSharp to Avalonia. Only runs once per (id,hue);
        // EntityCollectionData._cache memoises the result.
        public static IImage ToImageSource( this SKBitmap bmp )
        {
            if ( bmp == null )
            {
                return null;
            }

            try
            {
                using ( SKImage img = SKImage.FromBitmap( bmp ) )
                using ( SKData data = img.Encode( SKEncodedImageFormat.Png, 100 ) )
                using ( Stream stream = data.AsStream() )
                {
                    return new Avalonia.Media.Imaging.Bitmap( stream );
                }
            }
            catch
            {
                return null;
            }
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/interop-with-other-asynchronous-patterns-and-types?redirectedfrom=MSDN#WHToTap
        public static Task<bool> ToTask( this EventWaitHandle waitHandle, Func<bool> resultAction = null )
        {
            if ( waitHandle == null )
            {
                throw new ArgumentNullException( nameof( waitHandle ) );
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            RegisteredWaitHandle rwh = ThreadPool.RegisterWaitForSingleObject( waitHandle,
                delegate { tcs.TrySetResult( resultAction?.Invoke() ?? true ); }, null, -1, true );

            Task<bool> t = tcs.Task;

            t.ContinueWith( antecedent => rwh.Unregister( null ) );

            return t;
        }

        public static Task ToTask( this IEnumerable<EventWaitHandle> waitHandles )
        {
            List<Task<bool>> tasks = waitHandles.Select( waitHandle => waitHandle.ToTask() ).ToList();

            return Task.WhenAll( tasks );
        }

        public static string SHA1( this string str )
        {
            using ( System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create() )
            {
                byte[] hash = sha1.ComputeHash( Encoding.UTF8.GetBytes( str ) );
                StringBuilder formatted = new StringBuilder( 2 * hash.Length );

                foreach ( byte b in hash )
                {
                    formatted.AppendFormat( "{0:X2}", b );
                }

                return formatted.ToString();
            }
        }

        private static int GetNearestTabStop( int currentPosition, int tabLength )
        {
            // If already at the tab stop, jump to the next tab stop.
            if ( ( currentPosition % tabLength ) == 1 )
                currentPosition += tabLength;
            else
            {
                // If in the middle of two tab stops, move forward to the nearest.
                for ( int i = 0; i < tabLength; i++, currentPosition++ )
                    if ( ( currentPosition % tabLength ) == 1 )
                        break;
            }

            return currentPosition;
        }

        public static string TabsToSpaces( this string input, int tabLength )
        {
            if ( string.IsNullOrEmpty( input ) )
                return input;

            StringBuilder output = new StringBuilder();

            int positionInOutput = 1;
            foreach ( var c in input )
            {
                switch ( c )
                {
                    case '\t':
                        int spacesToAdd = GetNearestTabStop( positionInOutput, tabLength ) - positionInOutput;
                        output.Append( new string( ' ', spacesToAdd ) );
                        positionInOutput += spacesToAdd;
                        break;

                    case '\n':
                        output.Append( c );
                        positionInOutput = 1;
                        break;

                    default:
                        output.Append( c );
                        positionInOutput++;
                        break;
                }
            }
            return output.ToString();
        }
    }
}