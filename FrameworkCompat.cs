using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace NanoOsc;

internal static class FrameworkCompat
{
#if !NET8_0_OR_GREATER
    public static bool StartsWith(this string s, char c) => s.Length > 0 && s[0] == c;

    public static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> s, Span<byte> target)
    {
        fixed (char* chars = s)
        fixed (byte* bytes = target)
            return encoding.GetBytes(chars, s.Length, bytes, target.Length);
    }
    
    public static unsafe void WriteSingleBigEndian(Span<byte> target, float f) => MemoryMarshal.Cast<byte, int>(target[..4])[0] = BinaryPrimitives.ReverseEndianness(*(int*)&f);
    public static unsafe void WriteDoubleBigEndian(Span<byte> target, double d) => MemoryMarshal.Cast<byte, long>(target[..8])[0] = BinaryPrimitives.ReverseEndianness(*(long*)&d);
    
    public static unsafe float ReadSingleBigEndian(ReadOnlySpan<byte> target)
    {
        var asInt = MemoryMarshal.Cast<byte, int>(target[..4])[0];
        asInt = BinaryPrimitives.ReverseEndianness(asInt);
        return *(float*)&asInt;
    }
    
    public static unsafe double ReadDoubleBigEndian(ReadOnlySpan<byte> target)
    {
        var asInt = MemoryMarshal.Cast<byte, long>(target[..8])[0];
        asInt = BinaryPrimitives.ReverseEndianness(asInt);
        return *(double*)&asInt;
    }
#else
    public static void WriteSingleBigEndian(Span<byte> target, float f) => BinaryPrimitives.WriteSingleBigEndian(target, f);
    public static void WriteDoubleBigEndian(Span<byte> target, double f) => BinaryPrimitives.WriteDoubleBigEndian(target, f);
    
    public static float ReadSingleBigEndian(ReadOnlySpan<byte> target) => BinaryPrimitives.ReadSingleBigEndian(target);
    public static double ReadDoubleBigEndian(ReadOnlySpan<byte> target) => BinaryPrimitives.ReadDoubleBigEndian(target);
#endif
}