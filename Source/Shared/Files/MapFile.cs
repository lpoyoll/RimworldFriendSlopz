using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using MessagePack;
using Shared.Files.Configs.Mods;

namespace Shared.Files
{
    public class MapFile
    { 
        [IgnoreMember] public MapFileHeader Header;
        public string CurWeatherDefName { get; set; } = string.Empty;

        public ModsConfigFile Mods { get; set; } = null;

        public MapTileDetail[] Tiles { get; set; } = new MapTileDetail[0];

        public string[] FactionThings { get; set; } = null;

        public string[] NonFactionThings { get; set; } = null;

        public HumanFile[] FactionHumans { get; set; } = null;

        public HumanFile[] NonFactionHumans { get; set; } = null;

        public string[] FactionAnimals { get; set; } = null;

        public string[] NonFactionAnimals { get; set; } = null;

        public byte[] CompressIntoBytes()
        {
            var headerBytes = Serializer.ConvertObjectToBytes(Header);
            

            using (var ms = new MemoryStream())
            {
                ms.Write(BitConverter.GetBytes(headerBytes.Length), 0, 4);
                ms.Write(headerBytes, 0, headerBytes.Length);
                
                using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                {
                    Serializer.ConvertObjectToBytes(this, gzip);
                }

                return ms.ToArray();
            }
        }
        
        public static MapFile FullyDecompressFromBytes(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                using (var reader = new BinaryReader(ms))
                {
                    int headerLength = reader.ReadInt32();
                    byte[] headerBytes = reader.ReadBytes(headerLength);
                    var header = Serializer.ConvertBytesToObject<MapFileHeader>(headerBytes);

                    using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        var file = Serializer.ConvertBytesToObject<MapFile>(gzip);
                        file.Header = header;
                        return file;
                    }
                }
            }
        }
        
        /// <summary>
        /// Keeps the map compressed, where anything past after read is from the compressed map file.
        /// See <see cref="FullyDecompressFromBytes"/> to extract into a MapFile
        /// </summary>
        public static MapFileHeader FromBytes(byte[] bytes, out int read)
        {
            var spanBytes = bytes.AsSpan();
            var headerLength = BinaryPrimitives.ReadInt32LittleEndian(spanBytes.Slice(0, 4));
            read = sizeof(int);
            byte[] headerBytes = spanBytes.Slice(4, headerLength).ToArray();
            MapFileHeader header = Serializer.ConvertBytesToObject<MapFileHeader>(headerBytes);
            read += headerLength;
            return header;
        }
    }

    [MessagePackObject]
    public class MapFileHeader
    {
        [Key(0)] public int Tile {get; set;}
        [Key(1)] public int SizeX {get; set;}
        [Key(2)] public int SizeY {get; set;}
        [Key(3)] public int SizeZ {get; set;}
        [Key(4)] public int Wealth {get; set;}
        [Key(5)] public string Username {get; set;}
        
        [Key(6)] public string Password {get; set;}
    }
}