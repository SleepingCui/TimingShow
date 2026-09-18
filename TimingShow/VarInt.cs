using System.IO;

namespace TimingShow
{
    
    public static class VarInt
    {
        /// <summary>ZigZag：-1 -> 1，0 -> 0，15 -> 30</summary>
        public static uint ZigZag(int value) => (uint)((value << 1) ^ (value >> 31));

        /// <summary>ZigZag 逆变换：1 -> -1，0 -> 0，30 -> 15</summary>
        public static int UnZigZag(uint value) => (int)(value >> 1) ^ -(int)(value & 1);

        /// <summary>写入一个 ZigZag 变长整数</summary>
        public static void Write(BinaryWriter writer, int value)
        {
            uint v = ZigZag(value);
            while (v >= 0x80)
            {
                writer.Write((byte)(v | 0x80));
                v >>= 7;
            }
            writer.Write((byte)v);
        }

        /// <summary>读取一个 ZigZag 变长整数，与 <see cref="Write"/> 严格对称</summary>
        public static int Read(BinaryReader reader)
        {
            uint result = 0;
            int shift = 0;
            byte b;
            do
            {
                b = reader.ReadByte();
                result |= (uint)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            return UnZigZag(result);
        }
    }
}
