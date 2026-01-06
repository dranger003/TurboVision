using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Help;

/// <summary>
/// A window containing a help viewer with scrollbars.
/// </summary>
public class THelpWindow : TWindow
{
    public THelpWindow(THelpFile helpFile, int context)
        : base(new TRect(0, 0, 50, 18), HelpConstants.HelpWindowTitle, WindowConstants.wnNoNumber)
    {
        Options |= OptionFlags.ofCentered;

        var r = new TRect(0, 0, 50, 18);
        r = r.Grow(-2, -1);

        var hScrollBar = StandardScrollBar(ScrollBarParts.sbHorizontal | ScrollBarParts.sbHandleKeyboard);
        var vScrollBar = StandardScrollBar(ScrollBarParts.sbVertical | ScrollBarParts.sbHandleKeyboard);

        Insert(new THelpViewer(r, hScrollBar, vScrollBar, helpFile, context));
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(HelpConstants.HelpWindowPalette);
    }
}
