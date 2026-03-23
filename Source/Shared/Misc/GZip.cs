using System.IO;
using System.IO.Compression;

namespace Shared
{
    public static class GZip
    {
        public static byte[] CompressBytes(byte[] bytes)
        {
            MemoryStream memoryStream = new MemoryStream();
            GZipStream gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal);

            gzipStream.Write(bytes, 0, bytes.Length);
            return memoryStream.ToArray();
        }

        public static byte[] DecompressBytes(byte[] bytes)
        {
            MemoryStream memoryStream = new MemoryStream(bytes);
            MemoryStream outputStream = new MemoryStream();
            GZipStream decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress);

            decompressStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }
}
