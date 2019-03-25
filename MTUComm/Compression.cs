﻿using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Web;

namespace MTUComm
{
    public sealed class Compression
    {
        // GZip is simply deflate plus a checksum and header/footer. Deflate is faster and smaller
        // So naturally, no checksum is faster but then you also can't detect corrupt streams
        public enum ALGORITHM
        {
            DEFLATE,
            GZIP
        }

        public static string RandomString ( int length )
        {
            Random random = new Random();
        
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        
        public static string CompressToUrl ( string input, ALGORITHM algorithm = ALGORITHM.DEFLATE, int times = 1 )
        {
            return HttpUtility.UrlEncode ( Compress ( input, algorithm, times ) );
        }
        
        public static string Compress ( string input, ALGORITHM algorithm = ALGORITHM.DEFLATE, int times = 1 )
        {
            string result = Compress_Logic ( input, algorithm );
            
            if ( --times > 0 )
                return Compress ( result, algorithm, times );
            return result;
        }
        
        public static string Decompress ( string input, ALGORITHM algorithm = ALGORITHM.DEFLATE, int times = 1 )
        {
            string result = Decompress_Logic ( input, algorithm );
            
            if ( --times > 0 )
                return Decompress ( result, algorithm, times );
            return result;
        }
        
        private static string Compress_Logic ( string input, ALGORITHM algorithm )
        {
            using ( var outStream = new MemoryStream () )
            {
                dynamic stream = null;
                switch ( algorithm )
                {
                    case ALGORITHM.DEFLATE:
                        stream = new DeflateStream ( outStream, CompressionMode.Compress );
                        break;
                    case ALGORITHM.GZIP:
                        stream = new GZipStream ( outStream, CompressionMode.Compress );
                        break;
                }
                
                using ( stream )
                    using (var ms = new MemoryStream ( Encoding.UTF8.GetBytes ( input ) ) )
                        ms.CopyTo ( stream );
    
                return Convert.ToBase64String ( outStream.ToArray() );
            }
        }
    
        private static string Decompress_Logic ( string input, ALGORITHM algorithm )
        {
            using ( var inStream = new MemoryStream ( Convert.FromBase64String ( input ) ) )
            {
                dynamic stream = null;
                switch ( algorithm )
                {
                    case ALGORITHM.DEFLATE:
                        stream = new DeflateStream ( inStream, CompressionMode.Decompress );
                        break;
                    case ALGORITHM.GZIP:
                        stream = new GZipStream ( inStream, CompressionMode.Decompress );
                        break;
                }
                
                using ( stream )
                {
                    using ( var ms = new MemoryStream () )
                    {
                        stream.CopyTo ( ms );
                        return Encoding.UTF8.GetString ( ms.ToArray () );
                    }
                }
            }
        }
    }
}
