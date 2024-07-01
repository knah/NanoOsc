using System.Buffers.Binary;

namespace NanoOsc;

public ref struct OscBundleBuilder
{
    private readonly Memory<byte> myMemory;
    private readonly Span<byte> myBuffer;
    private OscMessageBuilder myLastMessageBuilder;
    private BundleMemberCookie myCookie;
    private int myOffset;

    public OscBundleBuilder(Memory<byte> buffer, long timestamp) : this(buffer.Span, timestamp)
    {
        myMemory = buffer;
    }

    public OscBundleBuilder(Span<byte> buffer, long timestamp)
    {
        myBuffer = buffer;
        myOffset = WriteBundleHeader(myBuffer, timestamp);
    }

    public OscMessageBuilder NextMessageBuilder(string address, int argumentCount)
    {
        TerminateCurrentBuilder();
        
        myCookie = new BundleMemberCookie(myBuffer.Slice(myOffset, 4));
        myOffset += 4;
        return myLastMessageBuilder = new OscMessageBuilder(myBuffer, address, argumentCount);
    }

    public Span<byte> Packet
    {
        get
        {
            TerminateCurrentBuilder();
            return myBuffer[..myOffset];
        }
    }
    
    public Memory<byte> PacketMemory
    {
        get
        {
            if (myMemory.Length == 0)
                throw new InvalidOperationException("Packet writer was created without memory");
            
            TerminateCurrentBuilder();
            return myMemory[..myOffset];
        }
    }

    private void TerminateCurrentBuilder()
    {
        if (myLastMessageBuilder.IsEmpty) return;
        myCookie.WriteLength(myLastMessageBuilder.Packet.Length);
        
        myCookie = default;
        myLastMessageBuilder = default;
    }

    private readonly ref struct BundleMemberCookie(Span<byte> target)
    {
        private readonly Span<byte> myTarget = target;

        public void WriteLength(int length)
        {
            BinaryPrimitives.WriteInt32BigEndian(myTarget, length);
        }
    }
    
    public static int WriteBundleHeader(Span<byte> target, long timestamp)
    {
        var position = 0;
        OscUtil.BundleIdentifier.CopyTo(target);
        position += OscUtil.BundleIdentifier.Length;

        BinaryPrimitives.WriteInt64BigEndian(target.Slice(position, 8), timestamp);
        return position + 8;
    }
}