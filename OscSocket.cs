using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace NanoOsc;

public sealed class OscSocket : IDisposable
{
    public IPEndPoint? RemoteAddress { get; set; }
    private readonly IPEndPoint myLocalAddress;
    private readonly IPEndPoint myReceiveRemote;
    private readonly ILogger<OscSocket>? myLogger;
    private readonly Socket mySocket;
    private readonly CancellationTokenSource myCancellationSource;
    private readonly CancellationToken myCancellationToken;

    public event OscPacketHandler<IPEndPoint>? OnPacket;
    public event OscMessageHandler<IPEndPoint>? OnMessage;

    public int Port => (mySocket.LocalEndPoint as IPEndPoint)?.Port ?? -1;

    public readonly Task ReaderTask;
    
    public OscSocket(IPEndPoint listenAddress, IPEndPoint? remoteAddress = null, ILogger<OscSocket>? logger = null)
    {
        myLocalAddress = listenAddress;
        myReceiveRemote = new IPEndPoint(listenAddress.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
        myLogger = logger;
        myCancellationSource = new CancellationTokenSource();
        myCancellationToken = myCancellationSource.Token;
        RemoteAddress = remoteAddress;
        mySocket = new Socket(listenAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        mySocket.Bind(listenAddress);
        myLocalAddress = (IPEndPoint?) mySocket.LocalEndPoint ?? myLocalAddress;
        ReaderTask = SocketLoop();
    }

    public ValueTask<int> Send(ReadOnlyMemory<byte> data, IPEndPoint? remote = null)
    {
        remote ??= RemoteAddress;
        if (remote == null) throw new ArgumentNullException(nameof(remote), "Must specify remote address for simple send operation!");
        return mySocket.SendToAsync(data, remote, myCancellationToken);
    }

    private async Task SocketLoop()
    {
        using var bufferOwner = MemoryPool<byte>.Shared.Rent(65536);
        var buffer = bufferOwner.Memory;
        while (!myCancellationToken.IsCancellationRequested)
        {
            var receiveResult = await mySocket.ReceiveMessageFromAsync(buffer, myReceiveRemote, myCancellationToken);
            var bytes = receiveResult.ReceivedBytes;
            if (receiveResult.RemoteEndPoint is not IPEndPoint source) continue;
            DispatchPacket(buffer.Span[..bytes], source);
        }
    }

    private void DispatchPacket(Span<byte> packet, IPEndPoint source)
    {
        OscParser parser;
        try
        {
            parser = new OscParser(packet);
        }
        catch (Exception ex)
        {
            myLogger?.LogError(ex, "Error while parsing packet received from {Remote}", source);
            return;
        }

        try
        {
            OnPacket?.Invoke(parser, source);
        }
        catch (Exception ex)
        {
            myLogger?.LogError(ex, "Error while calling packet handler for packet from {Remote}", source);
        }
        
        try
        {
            var messageHandler = OnMessage;
            if (messageHandler != null)
                OscParser.ParseMessages(parser, messageHandler, source);
        }
        catch (Exception ex)
        {
            myLogger?.LogError(ex, "Error while calling message handler for packet from {Remote}", source);
        }
    }

    public void Dispose()
    {
        myCancellationSource.Dispose();
        mySocket.Dispose();
    }
}