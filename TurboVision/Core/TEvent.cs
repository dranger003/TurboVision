using System.Runtime.InteropServices;

namespace TurboVision.Core;

/// <summary>
/// Represents an event in the TurboVision event system.
/// Can be a mouse, keyboard, or message event.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct TEvent
{
    [FieldOffset(0)]
    public ushort What;

    [FieldOffset(2)]
    public MouseEvent Mouse;

    [FieldOffset(2)]
    public KeyDownEvent KeyDown;

    [FieldOffset(2)]
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
    public static TEvent Command(ushort command, nint infoPtr = 0)
    {
        TEvent ev = default;
        ev.What = EventConstants.evCommand;
        ev.Message = new MessageEvent(command, infoPtr);
        return ev;
    }

    /// <summary>
    /// Creates a broadcast event.
    /// </summary>
    public static TEvent Broadcast(ushort command, nint infoPtr = 0)
    {
        TEvent ev = default;
        ev.What = EventConstants.evBroadcast;
        ev.Message = new MessageEvent(command, infoPtr);
        return ev;
    }
}
