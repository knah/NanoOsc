using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace NanoOsc;

public ref struct OscMessageBuilder
{
    private readonly Memory<byte> myMemory;
    private readonly Span<byte> myBuffer;
    private Span<OscType> myArgumentTypeBuffer;
    private int myOffset;

    public OscMessageBuilder(Memory<byte> buffer, string address, int argumentCount) : this(buffer.Span, address, argumentCount)
    {
        myMemory = buffer;
    }

    public OscMessageBuilder(Span<byte> buffer, string address, int argumentCount)
    {
        myBuffer = buffer;
        myOffset = WriteMessageHeader(buffer, address, argumentCount, out myArgumentTypeBuffer);
    }

    public bool IsEmpty => myBuffer.Length == 0;

    public void Write(float f)
    {
        PushArgumentType(OscType.Float);
        BinaryPrimitives.WriteSingleBigEndian(myBuffer[myOffset..], f);
        myOffset += 4;
    }
    
    public void Write(int i)
    {
        PushArgumentType(OscType.Int);
        BinaryPrimitives.WriteInt32BigEndian(myBuffer[myOffset..], i);
        myOffset += 4;
    }
    
    public void Write(ReadOnlySpan<byte> data)
    {
        PushArgumentType(OscType.Blob);
        BinaryPrimitives.WriteSingleBigEndian(myBuffer[myOffset..], data.Length);
        myOffset += 4;
        data.CopyTo(myBuffer[myOffset..]);
        myOffset += data.Length;
        AlignOffset();
    }
    
    public void Write(ReadOnlySpan<char> data)
    {
        PushArgumentType(OscType.String);
        var bytesWritten = Encoding.UTF8.GetBytes(data, myBuffer);
        myOffset += bytesWritten;
        AlignOffset();
    }

    public void Write(bool value) => PushArgumentType(value ? OscType.True : OscType.False);
    public void WriteNil() => PushArgumentType(OscType.Nil);

    public Span<byte> Packet
    {
        get
        {
            if (myArgumentTypeBuffer.Length > 0)
                throw new InvalidOperationException("Packet still has unfilled arguments, can't send");

            return myBuffer[..myOffset];
        }
    }
    
    public Memory<byte> PacketMemory
    {
        get
        {
            if (myArgumentTypeBuffer.Length > 0)
                throw new InvalidOperationException("Packet still has unfilled arguments, can't send");
            
            if (myMemory.Length == 0)
                throw new InvalidOperationException("Packet writer was created without memory");

            return myMemory[..myOffset];
        }
    }

    private void PushArgumentType(OscType type)
    {
        myArgumentTypeBuffer[0] = type;
        myArgumentTypeBuffer = myArgumentTypeBuffer[1..];
    }

    private void AlignOffset()
    {
        var alignedOffset = OscUtil.Align4(myOffset);
        myBuffer[myOffset..alignedOffset].Clear();
        myOffset = alignedOffset;
    }

    public static int WriteMessageHeader(Span<byte> target, string address, int numArguments, out Span<OscType> argumentsSpan)
    {
        if (!address.StartsWith('/'))
            throw new ArgumentException("Address must start with a forward slash /");

        var position = Encoding.UTF8.GetBytes(address, target);
        
        target[position++] = 0;
        while (position % 4 != 0)
            target[position++] = 0;

        target[position++] = (byte) ',';
        argumentsSpan = MemoryMarshal.Cast<byte, OscType>(target.Slice(position, numArguments));
        argumentsSpan.Fill((OscType)(byte) '!');

        position += numArguments;

        target[position++] = 0;
        while (position % 4 != 0)
            target[position++] = 0;

        return position;
    }

    public static Span<byte> SimplePacket(Span<byte> target, string address, float value)
    {
        var builder = new OscMessageBuilder(target, address, 1);
        builder.Write(value);
        return builder.Packet;
    }
    
    public static Memory<byte> SimplePacket(Memory<byte> target, string address, float value)
    {
        var builder = new OscMessageBuilder(target, address, 1);
        builder.Write(value);
        return builder.PacketMemory;
    }
    
    public static Span<byte> SimplePacket(Span<byte> target, string address, int value)
    {
        var builder = new OscMessageBuilder(target, address, 1);
        builder.Write(value);
        return builder.Packet;
    }
    
    public static Memory<byte> SimplePacket(Memory<byte> target, string address, int value)
    {
        var builder = new OscMessageBuilder(target, address, 1);
        builder.Write(value);
        return builder.PacketMemory;
    }
    
    public static Span<byte> SimplePacket(Span<byte> target, string address, bool value)
    {
        var builder = new OscMessageBuilder(target, address, 1);
        builder.Write(value);
        return builder.Packet;
    }
    
    public static Memory<byte> SimplePacket(Memory<byte> target, string address, bool value)
    {
        var builder = new OscMessageBuilder(target, address, 1);
        builder.Write(value);
        return builder.PacketMemory;
    }
}