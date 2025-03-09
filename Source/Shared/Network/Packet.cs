using System;

namespace Shared
{
    public class Packet
    {
        public static int DefaultPacketSizeInBytes { get; private set; } = 4;

        public static int CurrentPacketSizeInBytes { get; private set; }

        public string Header = string.Empty;

        public byte[] Contents = Array.Empty<byte>();

        public Packet(string header, byte[] contents)
        {
            this.Header = header;
            this.Contents = contents;
        }

        public static Packet CreateFromObject(string header, object objectToUse)
        {
            byte[] contents = Serializer.ConvertObjectToBytes(objectToUse);
            return new Packet(header, contents);
        }

        public static void SetPacketSize(int newSize) { CurrentPacketSizeInBytes = newSize; }

        public static byte[] CompressPacket(Packet packet) { return Serializer.ConvertObjectToBytes(packet, true); }

        public static Packet DecompressPacket(byte[] contents) { return Serializer.ConvertBytesToObject<Packet>(contents, true); }
    }
}