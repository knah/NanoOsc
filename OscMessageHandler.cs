namespace NanoOsc;

public delegate void OscPacketHandler<in TContext>(OscParser parser, TContext context);
public delegate void OscMessageHandler<in TContext>(OscMessageParser parser, TContext context);

public delegate void OscPacketHandler(OscParser parser);
public delegate void OscMessageHandler(OscMessageParser parser);