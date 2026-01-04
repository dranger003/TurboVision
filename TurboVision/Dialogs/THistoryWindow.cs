using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Popup window for history list display.
/// </summary>
public class THistoryWindow : TWindow
{
    private static readonly byte[] DefaultPalette = [0x13, 0x13, 0x15, 0x18, 0x17, 0x13, 0x14];

    public THistoryViewer? Viewer { get; protected set; }

    public THistoryWindow(TRect bounds, ushort historyId)
        : base(bounds, null, WindowConstants.wnNoNumber)
    {
        Flags = WindowFlags.wfClose;

        var r = GetExtent();
        r.Grow(-1, -1);
        Viewer = new THistoryViewer(
            r,
            StandardScrollBar(ScrollBarParts.sbHorizontal | ScrollBarParts.sbHandleKeyboard),
            StandardScrollBar(ScrollBarParts.sbVertical | ScrollBarParts.sbHandleKeyboard),
            historyId
        );
        Insert(Viewer);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public void GetSelection(Span<char> dest)
    {
        if (Viewer != null)
        {
            Viewer.GetText(dest, Viewer.Focused, (short)(dest.Length - 1));
        }
        else if (dest.Length > 0)
        {
            dest[0] = '\0';
        }
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == EventConstants.evMouseDown && !MouseInView(ev.Mouse.Where))
        {
            EndModal(CommandConstants.cmCancel);
            ClearEvent(ref ev);
        }
    }

    public override void ShutDown()
    {
        Viewer = null;
        base.ShutDown();
    }
}
