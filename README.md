# NanoOsc
Yet another library for building and parsing [Open Sound Control](https://en.wikipedia.org/wiki/Open_Sound_Control) packets.  
Aimed at being allocation-free, with built-in bidirectional socket and some utility classes to work with OSC.

## Supported features:
All of OSC 1.1 on-wire spec is supported. Array types are seemingly actually not arrays, and so are not really represented in the API.

## Example usage
### Receiving OSC messages
The provided `OscSocket` class provides simple UDP socket capable of sending and receiving OSC messages via UDP:
```csharp
var socket = new OscSocket(new IPEndPoint(IPAddress.Loopback, 0));
socket.OnMessage += (message, sourceAddress) => {
    if (message.Address.SequenceEqual("/space/param"u8)) {
        var f = message.ReadFloat();
        // ...
    }
}
```

### Parsing OSC message
If you have your own networking, or alternative transport protocol, you can parse your incoming data with `OscParser`:
```csharp
ReadOnlySpan<byte> incomingData = ...;
OscParser.ParseMessages(incomingData, (message, ctx) => {
    // message.Address ...
}, ctx);
// Alternatively:
var parser = new OscParser(incomingData);
if (parser.IsBundle) {
    // This has to be stored in a local variable (or a parameter) - calling parser.Bundle.NextMember would be an error
    var bundleParser = parser.Bundle;
    while (bundleParser.HasNextMember()) {
        OscParser nextMember = bundleParser.NextMember();
        // ...
    }
} else {
    var message = parser.Message;
    // message.Address ...
}
```

### Building OSC messages
NanoOsc doesn't allocate memory for you - which means you need to bring your own buffers.
```csharp
// This would be pooled memory in a production use case
var buffer = new byte[1024].AsMemory();
var builder = new OscMessageBuilder(buffer, "/space/param", 2);
builder.Write(4f);
builder.Write(false);
// PacketMemory is available if builder was constructed on top of memory, as in this sample
Memory<byte> dataToSend = builder.PacketMemory;
Span<byte> spanToSend = builder.Packet;
// Send it via your network channel, including OscSocket
```

### Routing OSC messages
> [!NOTE]
> This section is wishful thinking and not yet implemented.

Assuming one doesn't want to write a huge if-else block in a message handler, `OscRouter` can be used for a more data-driven approach:
```csharp
// TODO: there's no OscRouter yet!
```
