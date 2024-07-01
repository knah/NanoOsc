using System.Buffers.Binary;

namespace NanoOsc;

public ref struct OscBundleParser
{
    private readonly ReadOnlySpan<byte> myOscPacket;
    public readonly long BundleTimestamp;

    private int myCurrentInBodyOffset;
    

    public OscBundleParser(ReadOnlySpan<byte> packet)
    {
        myOscPacket = packet;
        if (!packet.StartsWith(OscUtil.BundleIdentifier))
            throw new ArgumentException("Input packet is not a bundle");
        
        BundleTimestamp = BinaryPrimitives.ReadInt64BigEndian(myOscPacket.Slice(OscUtil.BundleIdentifier.Length, 8));

        myCurrentInBodyOffset = OscUtil.BundleIdentifier.Length + 8;
    }
    
    public bool HasNextMember()
    {
        return myCurrentInBodyOffset < myOscPacket.Length;
    }

    public OscParser NextMember()
    {
        var subMessageLength = BinaryPrimitives.ReadInt32BigEndian(myOscPacket.Slice(myCurrentInBodyOffset, 4));
        var result = new OscParser(myOscPacket.Slice(myCurrentInBodyOffset + 4, subMessageLength));

        myCurrentInBodyOffset += 4 + subMessageLength;
        OscUtil.Align4(ref myCurrentInBodyOffset);

        return result;
    }

    public void Reset()
    {
        myCurrentInBodyOffset = OscUtil.BundleIdentifier.Length + 8;
    }
}