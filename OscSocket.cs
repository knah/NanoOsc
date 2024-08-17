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

    public 
        #if NETSTANDARD2_0
        Task<int>
        #else
        ValueTask<int>
        #endif
        Send(ReadOnlyMemory<byte> data, IPEndPoint? remote = null)
    {
        remote ??= RemoteAddress;
        if (remote == null) throw new ArgumentNullException(nameof(remote), "Must specify remote address for simple send operation!");
        #if NETSTANDARD2_0
        var arrayPool = ArrayPool<byte>.Shared;
        var buffer = arrayPool.Rent(data.Length);
        try
        {
            data.CopyTo(buffer);
            return mySocket.SendToAsync(new ArraySegment<byte>(buffer, 0, data.Length), SocketFlags.None, remote);
        }
        finally
        {
            arrayPool.Return(buffer);
        }
#else
        return mySocket.SendToAsync(data, remote, myCancellationToken);
        #endif
    }

    private async Task SocketLoop()
    {
        var arrayPool = ArrayPool<byte>.Shared;
        var buffer = arrayPool.Rent(65536);
        try
        {
            while (!myCancellationToken.IsCancellationRequested)
            {
                #if NETSTANDARD2_0
                var receiveResult = await mySocket.ReceiveMessageFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, myReceiveRemote);
                #else
                var receiveResult = await mySocket.ReceiveMessageFromAsync(buffer, myReceiveRemote, myCancellationToken);
                #endif
                var bytes = receiveResult.ReceivedBytes;
                if (receiveResult.RemoteEndPoint is not IPEndPoint source) continue;
                DispatchPacket(buffer.AsSpan(0, bytes), source);
            }
        }
        finally
        {
            arrayPool.Return(buffer);
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