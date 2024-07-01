namespace NanoOsc;

public class OscException : ApplicationException
{
    public OscException(string? message) : base(message)
    {
    }

    public OscException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}