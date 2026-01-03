using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Input history dropdown button.
/// </summary>
public class THistory : TView
{
    private static readonly byte[] DefaultPalette = [0x16, 0x17];
    private const string Icon = "▼";

    protected TInputLine? Link { get; set; }
    protected ushort HistoryId { get; set; }

    public THistory(TRect bounds, TInputLine? link, ushort historyId) : base(bounds)
    {
        Link = link;
        HistoryId = historyId;
        Options |= OptionFlags.ofPostProcess;
        EventMask |= EventConstants.evBroadcast;
    }

    public override void Draw()
    {
        var b = new TDrawBuffer();
        var color = GetColor(0x0101);

        b.MoveStr(0, Icon, color.Normal);
        WriteBuf(0, 0, Size.X, Size.Y, b);
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
            // TODO: Show history window
            ClearEvent(ref ev);
        }
        else if (ev.What == EventConstants.evKeyDown)
        {
            if (ev.KeyDown.KeyCode == KeyConstants.kbDown && Link != null)
            {
                // TODO: Show history window
                ClearEvent(ref ev);
            }
        }
    }

    public virtual void RecordHistory(string s)
    {
        // TODO: Add string to history
    }

    public override void ShutDown()
    {
        Link = null;
        base.ShutDown();
    }
}
