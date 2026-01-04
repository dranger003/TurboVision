namespace TurboVision.Core;

/// <summary>
/// Represents an event in the TurboVision event system.
/// Can be a mouse, keyboard, or message event.
/// </summary>
public struct TEvent
{
    public ushort What;

    // Event data - only one is valid based on What field
    // Using public fields to allow direct modification
    public MouseEvent Mouse;
    public KeyDownEvent KeyDown;
    public MessageEvent Message;

    /// <summary>
    /// Clears the event (marks as handled).
    /// </summary>
    public void Clear()
    {
        What = EventConstants.evNothing;
    }

    /// <summary>
    /// Checks if this is a mouse event.
    /// </summary>
    public readonly bool IsMouse
    {
        get { return (What & EventConstants.evMouse) != 0; }
    }

    /// <summary>
    /// Checks if this is a keyboard event.
    /// </summary>
    public readonly bool IsKeyboard
    {
        get { return (What & EventConstants.evKeyboard) != 0; }
    }

    /// <summary>
    /// Checks if this is a command or broadcast message.
    /// </summary>
    public readonly bool IsMessage
    {
        get { return (What & EventConstants.evMessage) != 0; }
    }

    /// <summary>
    /// Creates a command event.
    /// </summary>
    public static TEvent Command(ushort command, object? infoPtr = null)
    {
        TEvent ev = default;
        ev.What = EventConstants.evCommand;
        ev.Message = new MessageEvent(command, infoPtr);
        return ev;
    }

    /// <summary>
    /// Creates a broadcast event.
    /// </summary>
    public static TEvent Broadcast(ushort command, object? infoPtr = null)
    {
        TEvent ev = default;
        ev.What = EventConstants.evBroadcast;
        ev.Message = new MessageEvent(command, infoPtr);
        return ev;
    }
}
