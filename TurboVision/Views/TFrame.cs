using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Window border/title frame.
/// </summary>
public class TFrame : TView
{
    private static readonly byte[] DefaultPalette = [0x01, 0x01, 0x02, 0x02, 0x03];

    // Frame characters for drawing
    public static string FrameChars { get; set; } = "┌─┐│ │└─┘▒";
    public static string CloseIcon { get; set; } = "[■]";
    public static string ZoomIcon { get; set; } = "[↑]";
    public static string UnZoomIcon { get; set; } = "[↓]";
    public static string DragIcon { get; set; } = "───";
    public static string DragLeftIcon { get; set; } = "───";

    public TFrame(TRect bounds) : base(bounds)
    {
        GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        EventMask |= EventConstants.evBroadcast;
    }

    public override void Draw()
    {
        // TODO: Draw frame
        // This is a complex implementation involving:
        // - Drawing the border characters
        // - Drawing the title
        // - Drawing icons (close, zoom)
        var b = new TDrawBuffer();
        var color = GetColor(0x0101);

        // Fill with spaces for now
        b.MoveChar(0, ' ', color.Normal, Size.X);
        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evMouseDown)
        {
            // TODO: Handle frame drag, close icon click, zoom icon click
        }
        else if (ev.What == EventConstants.evBroadcast)
        {
            // TODO: Handle frame-related broadcasts
        }
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & (StateFlags.sfActive | StateFlags.sfDragging)) != 0)
        {
            DrawView();
        }
    }

    private void FrameLine(TDrawBuffer buf, int y, int n, TColorAttr color)
    {
        // TODO: Implement frame line drawing
    }

    private void DragWindow(TEvent ev, byte dragMode)
    {
        // TODO: Implement window dragging
    }
}
