using TurboVision.Application;
using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Window border/title frame.
/// </summary>
public class TFrame : TView
{
    private static readonly byte[] DefaultPalette = [0x01, 0x01, 0x02, 0x02, 0x03];

    // Frame characters: indexed by mask values built from initFrame
    // Format: " " + single-line chars + double-line chars
    // Index: 0=space, 3=└, 5=│, 6=┌, 7=├, 9=┘, 10=─, 11=┴, 12=┐, 13=┤, 14=┬, 15=┼
    // Index: 19=╚, 21=║, 22=╔, 23=╟, 25=╝, 26=═, 27=╧, 28=╗, 29=╢, 30=╤, 31=╪
    public static char[] FrameChars { get; set; } =
    [
        ' ', ' ', ' ', '└', ' ', '│', '┌', '├', ' ', '┘', '─', '┴', '┐', '┤', '┬', '┼',
        ' ', ' ', ' ', '╚', ' ', '║', '╔', '╟', ' ', '╝', '═', '╧', '╗', '╢', '╤', '╪'
    ];

    // InitFrame table for selecting frame character indices
    // [0-2]: inactive top row (left, middle, right)
    // [3-5]: inactive middle rows (left, middle, right)
    // [6-8]: inactive bottom row (left, middle, right)
    // [9-17]: active frame (same pattern, double-line characters)
    public static byte[] InitFrame { get; set; } =
    [
        0x06, 0x0A, 0x0C, // inactive: top-left, top-middle, top-right
        0x05, 0x00, 0x05, // inactive: left, middle, right
        0x03, 0x0A, 0x09, // inactive: bottom-left, bottom-middle, bottom-right
        0x16, 0x1A, 0x1C, // active: top-left, top-middle, top-right
        0x15, 0x00, 0x15, // active: left, middle, right
        0x13, 0x1A, 0x19  // active: bottom-left, bottom-middle, bottom-right
    ];

    public static string CloseIcon { get; set; } = "[■]";
    public static string ZoomIcon { get; set; } = "[↑]";
    public static string UnZoomIcon { get; set; } = "[↓]";
    public static string DragIcon { get; set; } = "───";
    public static string DragLeftIcon { get; set; } = "───";

    public TFrame(TRect bounds) : base(bounds)
    {
        GrowMode = GrowFlags.gfGrowHiX | GrowFlags.gfGrowHiY;
        EventMask |= EventConstants.evBroadcast | EventConstants.evMouseUp;
    }

    public override void Draw()
    {
        TAttrPair cFrame, cTitle;
        int f;

        if (GetState(StateFlags.sfDragging))
        {
            cFrame = GetColor(0x0505);
            cTitle = GetColor(0x0005);
            f = 0;
        }
        else if (!GetState(StateFlags.sfActive))
        {
            cFrame = GetColor(0x0101);
            cTitle = GetColor(0x0002);
            f = 0;
        }
        else
        {
            cFrame = GetColor(0x0503);
            cTitle = GetColor(0x0004);
            f = 9;
        }

        int width = Size.X;
        int l = width - 10;

        var window = Owner as TWindow;
        if (window != null && (window.Flags & (WindowFlags.wfClose | WindowFlags.wfZoom)) != 0)
        {
            l -= 6;
        }

        var b = new TDrawBuffer();

        // Draw top line
        FrameLine(b, 0, f, cFrame.Normal);

        // Add window number if present
        if (window != null && window.Number != WindowConstants.wnNoNumber && window.Number < 10)
        {
            l -= 4;
            int i = (window.Flags & WindowFlags.wfZoom) != 0 ? 7 : 3;
            b.PutChar(width - i, (char)('0' + window.Number));
        }

        // Draw title
        if (window != null)
        {
            string? title = window.GetTitle((short)l);
            if (!string.IsNullOrEmpty(title))
            {
                int titleLen = Math.Min(title.Length, width - 10);
                titleLen = Math.Max(titleLen, 0);
                int i = (width - titleLen) >> 1;
                b.PutChar(i - 1, ' ');
                b.MoveStr(i, title.AsSpan(0, titleLen), cTitle.Normal);
                b.PutChar(i + titleLen, ' ');
            }
        }

        // Add close/zoom icons if active
        if (GetState(StateFlags.sfActive) && window != null)
        {
            if ((window.Flags & WindowFlags.wfClose) != 0)
            {
                b.MoveCStr(2, CloseIcon, cFrame);
            }
            if ((window.Flags & WindowFlags.wfZoom) != 0)
            {
                TPoint minSize, maxSize;
                window.SizeLimits(out minSize, out maxSize);
                if (window.Size.X == maxSize.X && window.Size.Y == maxSize.Y)
                {
                    b.MoveCStr(width - 5, UnZoomIcon, cFrame);
                }
                else
                {
                    b.MoveCStr(width - 5, ZoomIcon, cFrame);
                }
            }
        }

        WriteLine(0, 0, Size.X, 1, b);

        // Draw middle lines
        for (int y = 1; y < Size.Y - 1; y++)
        {
            FrameLine(b, y, f + 3, cFrame.Normal);
            WriteLine(0, y, Size.X, 1, b);
        }

        // Draw bottom line
        FrameLine(b, Size.Y - 1, f + 6, cFrame.Normal);

        // Add drag icons if active and growable
        if (GetState(StateFlags.sfActive) && window != null && (window.Flags & WindowFlags.wfGrow) != 0)
        {
            b.MoveCStr(0, DragLeftIcon, cFrame);
            b.MoveCStr(width - 2, DragIcon, cFrame);
        }

        WriteLine(0, Size.Y - 1, Size.X, 1, b);
    }

    private void FrameLine(TDrawBuffer frameBuf, int y, int n, TColorAttr color)
    {
        int width = Size.X;

        // Simple frame line without child view framing (for now)
        char leftChar = FrameChars[InitFrame[n]];
        char middleChar = FrameChars[InitFrame[n + 1]];
        char rightChar = FrameChars[InitFrame[n + 2]];

        frameBuf.PutChar(0, leftChar);
        frameBuf.PutAttribute(0, color);

        for (int x = 1; x < width - 1; x++)
        {
            frameBuf.PutChar(x, middleChar);
            frameBuf.PutAttribute(x, color);
        }

        frameBuf.PutChar(width - 1, rightChar);
        frameBuf.PutAttribute(width - 1, color);
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

    private void DragWindow(TEvent ev, byte dragMode)
    {
        // TODO: Implement window dragging
    }
}
