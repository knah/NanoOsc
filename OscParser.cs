using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;

namespace NanoOsc;

public ref struct OscParser
{
    private readonly ReadOnlySpan<byte> myOscPacket;
    public readonly bool IsBundle;

    public OscParser(ReadOnlySpan<byte> oscPacket)
    {
        myOscPacket = oscPacket;
        if (oscPacket.Length <= 0)
            throw new EndOfStreamException("OSC packet is empty");

        switch ((char)oscPacket[0])
        {
            case '/':
                IsBundle = false;
                break;
            case '#':
                IsBundle = true;
                break;
            default:
                throw new OscException($"Unknown classifier byte {oscPacket[0]}");
        }
    }

    public OscBundleParser Bundle => new(myOscPacket);
    public OscMessageParser Message => new(myOscPacket);

    public static void ParseMessages(ReadOnlySpan<byte> packet, OscMessageHandler handler) => 
        ParseMessages(packet, static (message, handler) => handler(message), handler);

    public static void ParseMessages<TContext>(ReadOnlySpan<byte> packet, OscMessageHandler<TContext> handler, TContext context)
    {
        var rootParser = new OscParser(packet);
        ParseMessages(rootParser, handler, context);
    }

    public static void ParseMessages(OscParser rootParser, OscMessageHandler handler) =>
        ParseMessages(rootParser, static (parser, handler) => handler(parser), handler);
    
    public static void ParseMessages<TContext>(OscParser rootParser, OscMessageHandler<TContext> handler, TContext context)
    {
        if (!rootParser.IsBundle)
        {
            handler(rootParser.Message, context);
            return;
        }

        var bundleParser = rootParser.Bundle;
        while (bundleParser.HasNextMember())
        {
            var nextMessage = bundleParser.NextMember();
            ParseMessages(nextMessage, handler, context);
        }
    }
}