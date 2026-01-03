namespace TurboVision.Core;

/// <summary>
/// Represents a message/command event.
/// </summary>
public struct MessageEvent
{
    public ushort Command { get; set; }
    public nint InfoPtr { get; set; }

    public int InfoInt
    {
        get { return (int)InfoPtr; }
        set { InfoPtr = value; }
    }

    public MessageEvent(ushort command)
    {
        Command = command;
        InfoPtr = 0;
    }

    public MessageEvent(ushort command, nint infoPtr)
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
