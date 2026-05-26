using System;
using System.IO;
using System.IO.Compression;

namespace ClassicAssist.UO
{
    // Wraps .NET's ZLibStream (RFC 1950: deflate + zlib header + adler32),
    // which is the same wire format the native zlib `compress` / `uncompress`
    // entry points produce. Was previously a pair of zlib32.dll / zlib64.dll
    // PInvokes; the managed implementation runs identically on Linux/macOS.
    public static class Compression
    {
        public static bool Uncompress( ref byte[] destBuffer, ref int destLength, byte[] sourceBuffer, int sourceLen )
        {
            try
            {
                using ( MemoryStream src = new MemoryStream( sourceBuffer, 0, sourceLen, writable: false ) )
                using ( ZLibStream zs = new ZLibStream( src, CompressionMode.Decompress ) )
                {
                    int capacity = destLength;
                    int total = 0;

                    while ( total < capacity )
                    {
                        int read = zs.Read( destBuffer, total, capacity - total );
                        if ( read == 0 )
                        {
                            break;
                        }
                        total += read;
                    }

                    // Match zlib's `uncompress` contract: destLength is in/out.
                    // On success it reports the actual decompressed byte count.
                    destLength = total;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static byte[] Compress( byte[] bytes )
        {
            using ( MemoryStream dest = new MemoryStream( bytes.Length + 32 ) )
            {
                using ( ZLibStream zs = new ZLibStream( dest, CompressionLevel.Optimal, leaveOpen: true ) )
                {
                    zs.Write( bytes, 0, bytes.Length );
                }

                return dest.ToArray();
            }
        }
    }
}
