using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SkiaSharp;

namespace ClassicAssist.UO.Data
{
    public static class Hues
    {
        private const int BlockCount = 375;
        private static string _dataPath;

        public static Lazy<HueEntry[]> _lazyHueEntries = new Lazy<HueEntry[]>( LoadHueIndex );

        public static bool Initialize( string dataPath )
        {
            _dataPath = dataPath;

            return true;
        }

        private static HueEntry[] LoadHueIndex()
        {
            HueEntry[] entries = new HueEntry[3000];

            if ( !File.Exists( Path.Combine( _dataPath, "hues.mul" ) ) )
            {
                throw new FileNotFoundException( "File not found", "hues.mul" );
            }

            using ( FileStream reader = File.Open( Path.Combine( _dataPath, "hues.mul" ), FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite ) )
            {
                BinaryReader binaryReader = new BinaryReader( reader );
                int total = 0;

                for ( int i = 0; i < BlockCount; i++ )
                {
                    binaryReader.ReadInt32();

                    for ( int j = 0; j < 8; ++j, ++total )
                    {
                        entries[total] = HueEntry.Read( binaryReader );
                    }
                }
            }

            return entries;
        }

        // Operates on Bgra8888 buffers. Recovers the source's R5 from R8 via
        // `>> 3` (the 5->8 replicate-bit expand is bijective on the 32
        // reachable 8-bit values, so this round-trips exactly back to the
        // original 1555 R channel).
        public static unsafe void ApplyHue( int hue, SKBitmap bmp, bool onlyHueGrayPixels )
        {
            if ( bmp.ColorType != SKColorType.Bgra8888 )
            {
                throw new ArgumentException( $"ApplyHueSk requires Bgra8888, got {bmp.ColorType}", nameof( bmp ) );
            }

            hue = ( hue & 0x3FFF ) - 1;
            HueEntry hueEntry = _lazyHueEntries.Value[hue];
            uint[] palette = hueEntry.ColorsBgra;

            int width = bmp.Width;
            int height = bmp.Height;
            int rowBytes = bmp.RowBytes;
            int rowPixels = rowBytes >> 2;
            int delta = rowPixels - width;

            uint* pBuffer = (uint*) bmp.GetPixels().ToPointer();
            uint* pLineEnd = pBuffer + width;
            uint* pImageEnd = pBuffer + rowPixels * height;

            if ( onlyHueGrayPixels )
            {
                while ( pBuffer < pImageEnd )
                {
                    while ( pBuffer < pLineEnd )
                    {
                        uint c = *pBuffer;

                        // Bgra8888 little-endian byte order: B G R A. uint = (A<<24)|(R<<16)|(G<<8)|B.
                        if ( ( c & 0xFF000000u ) != 0 )
                        {
                            byte b = (byte) ( c & 0xFF );
                            byte g = (byte) ( ( c >> 8 ) & 0xFF );
                            byte r = (byte) ( ( c >> 16 ) & 0xFF );

                            if ( r == g && r == b )
                            {
                                *pBuffer = palette[r >> 3];
                            }
                        }

                        ++pBuffer;
                    }

                    pBuffer += delta;
                    pLineEnd += rowPixels;
                }
            }
            else
            {
                while ( pBuffer < pImageEnd )
                {
                    while ( pBuffer < pLineEnd )
                    {
                        uint c = *pBuffer;

                        if ( ( c & 0xFF000000u ) != 0 )
                        {
                            byte r = (byte) ( ( c >> 16 ) & 0xFF );
                            *pBuffer = palette[r >> 3];
                        }

                        ++pBuffer;
                    }

                    pBuffer += delta;
                    pLineEnd += rowPixels;
                }
            }
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct HueEntry
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 32 )]
        public short[] Colors;

        // Bgra8888 mirror of Colors, populated once at load. Pre-converted so
        // ApplyHueSk's inner loop stays branchless on the colour math.
        public uint[] ColorsBgra;

        public short TableStart;
        public short TableEnd;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 20 )]
        public string Name;

        public static HueEntry Read( BinaryReader reader )
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            HueEntry entry = new HueEntry();

            entry.Colors = new short[32];
            entry.ColorsBgra = new uint[32];

            for ( int i = 0; i < 32; ++i )
            {
                ushort c1555 = (ushort) ( reader.ReadUInt16() | 0x8000 );
                entry.Colors[i] = (short) c1555;
                entry.ColorsBgra[i] = Convert1555ToBgra8888( c1555 );
            }

            entry.TableStart = (short) ( reader.ReadUInt16() | 0x8000 );
            entry.TableEnd = (short) ( reader.ReadUInt16() | 0x8000 );
            entry.Name = Encoding.ASCII.GetString( reader.ReadBytes( 20 ) ).TrimEnd( '\0' );

            return entry;
        }

        // Replicate-bit expand 5->8 (0..31 -> 0..255, 31->255). Alpha bit in
        // 1555 means "opaque" once the loader XORs 0x8000; we encode that as
        // 0xFF in BGRA. A 1555 zero stays as BGRA zero (fully transparent).
        internal static uint Convert1555ToBgra8888( ushort c )
        {
            if ( c == 0 )
            {
                return 0;
            }

            int r5 = ( c >> 10 ) & 0x1F;
            int g5 = ( c >> 5 ) & 0x1F;
            int b5 = c & 0x1F;

            uint r = (uint) ( ( r5 << 3 ) | ( r5 >> 2 ) );
            uint g = (uint) ( ( g5 << 3 ) | ( g5 >> 2 ) );
            uint b = (uint) ( ( b5 << 3 ) | ( b5 >> 2 ) );

            return ( 0xFFu << 24 ) | ( r << 16 ) | ( g << 8 ) | b;
        }
    }
}