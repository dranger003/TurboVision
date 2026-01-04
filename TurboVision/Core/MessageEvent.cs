namespace TurboVision.Core;

/// <summary>
/// Represents a message/command event.
/// </summary>
public struct MessageEvent
{
    public ushort Command { get; set; }
    public object? InfoPtr { get; set; }

    public int InfoInt
    {
        get { return InfoPtr is int i ? i : (InfoPtr is nint n ? (int)n : 0); }
        set { InfoPtr = value; }
    }

    public nint InfoNInt
    {
        get { return InfoPtr is nint n ? n : (InfoPtr is int i ? i : 0); }
        set { InfoPtr = value; }
    }

    public MessageEvent(ushort command)
    {
        Command = command;
        InfoPtr = null;
    }

    public MessageEvent(ushort command, object? infoPtr)
    {
        Command = command;
        InfoPtr = infoPtr;
    }

    public MessageEvent(ushort command, int infoInt)
    {
        Command = command;
        InfoPtr = infoInt;
    }
}
