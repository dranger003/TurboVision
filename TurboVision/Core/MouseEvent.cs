namespace TurboVision.Core;

/// <summary>
/// Represents a mouse event.
/// </summary>
public record struct MouseEvent
{
    public TPoint Where { get; set; }
    public ushort EventFlags { get; set; }
    public ushort ControlKeyState { get; set; }
    public byte Buttons { get; set; }
    public byte Wheel { get; set; }

    public bool DoubleClick
    {
        get { return (EventFlags & EventConstants.meDoubleClick) != 0; }
    }

    public bool TripleClick
    {
        get { return (EventFlags & EventConstants.meTripleClick) != 0; }
    }
}
