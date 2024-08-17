using System.Buffers.Binary;

namespace NanoOsc;

public ref struct OscMessageParser
{
    private readonly ReadOnlySpan<byte> myOscPacket;

    public readonly ReadOnlySpan<byte> Address;
    public readonly ReadOnlySpan<byte> TypeString;

    private readonly int myBodyStartOffset;

    private int myCurrentTypeOffset;
    private int myCurrentInBodyOffset;

    public OscMessageParser(ReadOnlySpan<byte> oscPacket)
    {
        if (oscPacket.Length <= 0)
            throw new EndOfStreamException("OSC packet is empty");

        if (oscPacket[0] != '/')
            throw new ArgumentException("Input packet is not a message");
        
        myOscPacket = oscPacket;

        var firstNulByte = myOscPacket.IndexOf((byte)0);
        if (firstNulByte < 0) throw new OscException("Address is not 0-terminated");

        Address = myOscPacket[..firstNulByte];
        var argsStartOffset = OscUtil.Align4(firstNulByte + 1);

        var hasArgsString = myOscPacket[argsStartOffset] == ',';
        if (!hasArgsString)
            throw new OscException("Messages without type string are not supported");

        var argsStringEnd = myOscPacket[argsStartOffset..].IndexOf((byte)0);
        if (argsStringEnd < 0)
            throw new OscException("Type string is not 0-terminated");

        argsStringEnd += argsStartOffset;

        TypeString = myOscPacket[(argsStartOffset + 1)..argsStringEnd];

        myBodyStartOffset = myCurrentInBodyOffset = OscUtil.Align4(argsStringEnd + 1);
    }

    public void Reset()
    {
        myCurrentTypeOffset = 0;
        myCurrentInBodyOffset = myBodyStartOffset;
    }

    public OscType PeekNextRawType()
    {
        if (myCurrentTypeOffset >= TypeString.Length)
            return 0;

        return (OscType)TypeString[myCurrentTypeOffset];
    }


    public Type? PeekNextType()
    {
        switch (PeekNextRawType())
        {
            case 0:
                return null;
            case OscType.Int:
            case OscType.Char:
            case OscType.Color:
            case OscType.Midi:
                return typeof(int);
            case OscType.Float:
                return typeof(float);
            case OscType.Long:
            case OscType.Timestamp:
                return typeof(long);
            case OscType.Double:
                return typeof(double);
            case OscType.True:
            case OscType.False:
                return typeof(bool);
            case OscType.Nil:
            case OscType.Impulse:
            case OscType.ArrayStart:
            case OscType.ArrayEnd:
                return typeof(void);
            case OscType.String:
            case OscType.Symbol:
                return typeof(string);
            case OscType.Blob:
                return typeof(byte[]);
            default:
                return null;
        }
    }

    public int ReadInt()
    {
        if (PeekNextType() != typeof(int))
            throw new OscException($"Next type is {PeekNextType()}, not int!");

        var result = BinaryPrimitives.ReadInt32BigEndian(myOscPacket.Slice(myCurrentInBodyOffset, 4));

        myCurrentTypeOffset++;
        myCurrentInBodyOffset += 4;

        return result;
    }

    public long ReadLong()
    {
        if (PeekNextType() != typeof(long))
            throw new OscException($"Next type is {PeekNextType()}, not long!");

        var result = BinaryPrimitives.ReadInt64BigEndian(myOscPacket.Slice(myCurrentInBodyOffset, 8));

        myCurrentTypeOffset++;
        myCurrentInBodyOffset += 8;

        return result;
    }

    public float ReadFloat()
    {
        if (PeekNextType() != typeof(float))
            throw new OscException($"Next type is {PeekNextType()}, not float!");

        var result = FrameworkCompat.ReadSingleBigEndian(myOscPacket.Slice(myCurrentInBodyOffset, 4));

        myCurrentTypeOffset++;
        myCurrentInBodyOffset += 4;

        return result;
    }

    public double ReadDouble()
    {
        if (PeekNextType() != typeof(double))
            throw new OscException($"Next type is {PeekNextType()}, not double!");

        var result = FrameworkCompat.ReadDoubleBigEndian(myOscPacket.Slice(myCurrentInBodyOffset, 8));

        myCurrentTypeOffset++;
        myCurrentInBodyOffset += 8;

        return result;
    }

    public void SkipArrayTag()
    {
        var nextType = PeekNextRawType();
        if (nextType != OscType.ArrayStart && nextType != OscType.ArrayEnd)
            throw new OscException($"Next type is {PeekNextRawType()}, not array!");

        myCurrentTypeOffset++;
    }
    
    public void SkipImpulse()
    {
        var nextType = PeekNextRawType();
        if (nextType != OscType.Impulse)
            throw new OscException($"Next type is {PeekNextRawType()}, not impulse!");

        myCurrentTypeOffset++;
    }

    public ReadOnlySpan<byte> ReadBlob()
    {
        if (PeekNextType() != typeof(byte[]))
            throw new OscException($"Next type is {PeekNextType()}, not blob!");

        var length = BinaryPrimitives.ReadInt32BigEndian(myOscPacket.Slice(myCurrentInBodyOffset, 4));
        var result = myOscPacket.Slice(myCurrentInBodyOffset + 4, length);

        myCurrentTypeOffset++;
        myCurrentInBodyOffset += 4 + length;
        OscUtil.Align4(ref myCurrentInBodyOffset);

        return result;
    }

    public ReadOnlySpan<byte> ReadString()
    {
        if (PeekNextType() != typeof(string))
            throw new OscException($"Next type is {PeekNextType()}, not string!");

        var nextZero = myOscPacket[myCurrentInBodyOffset..].IndexOf((byte)0);
        if (nextZero < 0)
            throw new OscException("No zero byte in string");
        var result = myOscPacket.Slice(myCurrentInBodyOffset, nextZero);

        myCurrentTypeOffset++;
        myCurrentInBodyOffset += nextZero;
        OscUtil.Align4(ref myCurrentInBodyOffset);

        return result;
    }

    public bool ReadBool()
    {
        if (PeekNextType() != typeof(bool))
            throw new OscException($"Next type is {PeekNextType()}, not bool!");

        var result = PeekNextRawType() == OscType.True;

        myCurrentTypeOffset++;

        return result;
    }
}