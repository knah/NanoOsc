namespace NanoOsc;

internal static class OscUtil
{
    public static void Align4(ref int offset)
    {
        checked
        {
            var remainder = offset % 4;
            if (remainder != 0) offset += 4 - remainder;
        }
    }

    public static int Align4(int offset)
    {
        Align4(ref offset);
        return offset;
    }

    public static ReadOnlySpan<byte> BundleIdentifier => "#bundle\0"u8;
}