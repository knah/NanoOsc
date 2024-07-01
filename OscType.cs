namespace NanoOsc;

public enum OscType : byte
{
    None,
    
    Int = (byte)'i',
    Float = (byte)'f',
    String = (byte)'s',
    Blob = (byte)'b',
    
    Char = (byte)'c',
    Symbol = (byte)'S',
    Double = (byte)'d',
    Color = (byte)'r',
    Midi = (byte)'m',
    Long = (byte)'h',
    Timestamp = (byte)'t',
    
    True = (byte)'T',
    False = (byte)'F',
    Nil = (byte)'N',
    Impulse = (byte)'I',
    
    ArrayStart = (byte)'[',
    ArrayEnd = (byte)']',
}