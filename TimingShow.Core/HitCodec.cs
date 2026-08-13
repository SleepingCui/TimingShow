using System;
using System.IO;

namespace TimingShow
{
    public static class HitCodec
    {
        public static void WriteHit(BinaryWriter writer, double timing, int marginCode, ref long prevTimingBits)
        {
            long bits = BitConverter.DoubleToInt64Bits(timing);
            writer.Write(bits ^ prevTimingBits);
            prevTimingBits = bits;
            WriteVarInt(writer, marginCode);
        }
        
        public static void WriteVarInt(BinaryWriter writer, int value)
        {
            uint v = (uint)value;
            while (v >= 0x80)
            {
                writer.Write((byte)(v | 0x80));
                v >>= 7;
            }
            writer.Write((byte)v);
        }
        
    }
}
