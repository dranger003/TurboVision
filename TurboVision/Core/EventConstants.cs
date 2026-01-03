namespace TurboVision.Core;

/// <summary>
/// Event type constants.
/// </summary>
public static class EventConstants
{
    // Event codes
    public const ushort evNothing = 0x0000;
    public const ushort evMouseDown = 0x0001;
    public const ushort evMouseUp = 0x0002;
    public const ushort evMouseMove = 0x0004;
    public const ushort evMouseAuto = 0x0008;
    public const ushort evKeyDown = 0x0010;
    public const ushort evMouseWheel = 0x0020;
    public const ushort evCommand = 0x0100;
    public const ushort evBroadcast = 0x0200;

    // Event masks
    public const ushort evMouse = 0x002F;
    public const ushort evKeyboard = 0x0010;
    public const ushort evMessage = 0xFF00;

    // Mouse button state masks
    public const byte mbLeftButton = 0x01;
    public const byte mbRightButton = 0x02;
    public const byte mbMiddleButton = 0x04;

    // Mouse wheel state masks
    public const byte mwUp = 0x01;
    public const byte mwDown = 0x02;
    public const byte mwLeft = 0x04;
    public const byte mwRight = 0x08;

    // Mouse event flags
    public const byte meMouseMoved = 0x01;
    public const byte meDoubleClick = 0x02;
    public const byte meTripleClick = 0x04;

    // Positional and focused event masks
    public const ushort positionalEvents = evMouse & ~evMouseWheel;
    public const ushort focusedEvents = evKeyboard | evCommand;
}
