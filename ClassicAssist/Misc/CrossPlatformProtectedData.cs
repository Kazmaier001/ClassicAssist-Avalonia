using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ClassicAssist.Misc
{
    /// <summary>
    /// Cross-platform replacement for Windows DPAPI (ProtectedData).
    /// Uses AES with a machine-specific key derived from environment info.
    /// </summary>
    public static class CrossPlatformProtectedData
    {
        private static byte[] GetMachineKey()
        {
            // Derive a key from machine name + user name as a simple machine-bound key
            string seed = Environment.MachineName + "|" + Environment.UserName;
            using ( var sha = SHA256.Create() )
            {
                return sha.ComputeHash( Encoding.UTF8.GetBytes( seed ) );
            }
        }

        public static byte[] Protect( byte[] data )
        {
            if ( data == null ) throw new ArgumentNullException( nameof( data ) );
            using ( var aes = Aes.Create() )
            {
                aes.Key = GetMachineKey();
                aes.GenerateIV();

                using ( var encryptor = aes.CreateEncryptor() )
                using ( var ms = new MemoryStream() )
                {
                    ms.Write( aes.IV, 0, aes.IV.Length );
                    using ( var cs = new CryptoStream( ms, encryptor, CryptoStreamMode.Write ) )
                    {
                        cs.Write( data, 0, data.Length );
                    }
                    return ms.ToArray();
                }
            }
        }

        public static byte[] Unprotect( byte[] data )
        {
            if ( data == null ) throw new ArgumentNullException( nameof( data ) );
            using ( var aes = Aes.Create() )
            {
                aes.Key = GetMachineKey();
                byte[] iv = new byte[16];
                Array.Copy( data, 0, iv, 0, 16 );
                aes.IV = iv;

                using ( var decryptor = aes.CreateDecryptor() )
                using ( var ms = new MemoryStream( data, 16, data.Length - 16 ) )
                using ( var cs = new CryptoStream( ms, decryptor, CryptoStreamMode.Read ) )
                using ( var output = new MemoryStream() )
                {
                    cs.CopyTo( output );
                    return output.ToArray();
                }
            }
        }
    }

    // Stub for compatibility
    public enum DataProtectionScope { CurrentUser, LocalMachine }

    public static class ProtectedData
    {
        public static byte[] Protect( byte[] userData, byte[] optionalEntropy, DataProtectionScope scope )
        {
            return CrossPlatformProtectedData.Protect( userData );
        }

        public static byte[] Unprotect( byte[] encryptedData, byte[] optionalEntropy, DataProtectionScope scope )
        {
            return CrossPlatformProtectedData.Unprotect( encryptedData );
        }
    }
}