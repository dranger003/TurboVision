using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// List viewer for history dropdown.
/// </summary>
public class THistoryViewer : TListViewer
{
    private static readonly byte[] DefaultPalette = [0x06, 0x06, 0x07, 0x06, 0x06];

    public ushort HistoryId { get; }

    public THistoryViewer(TRect bounds, TScrollBar? hScrollBar, TScrollBar? vScrollBar, ushort historyId)
        : base(bounds, 1, hScrollBar, vScrollBar)
    {
        HistoryId = historyId;
        SetRange((short)THistoryList.HistoryCount(historyId));
        if (Range > 1)
        {
            FocusItem(1);
        }
        if (HScrollBar != null)
        {
            HScrollBar.SetRange(0, HistoryWidth() - Size.X + 3);
        }
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void GetText(Span<char> dest, short item, short maxLen)
    {
        var str = THistoryList.HistoryStr(HistoryId, item);
        if (str != null)
        {
            int len = Math.Min(str.Length, Math.Min(maxLen, dest.Length - 1));
            str.AsSpan(0, len).CopyTo(dest);
            if (len < dest.Length)
            {
                dest[len] = '\0';
            }
        }
        else if (dest.Length > 0)
        {
            dest[0] = '\0';
        }
    }

    public override void HandleEvent(ref TEvent ev)
    {
        if ((ev.What == EventConstants.evMouseDown && (ev.Mouse.EventFlags & EventConstants.meDoubleClick) != 0) ||
            (ev.What == EventConstants.evKeyDown && ev.KeyDown.KeyCode == KeyConstants.kbEnter))
        {
            EndModal(CommandConstants.cmOK);
            ClearEvent(ref ev);
        }
        else if ((ev.What == EventConstants.evKeyDown && ev.KeyDown.KeyCode == KeyConstants.kbEsc) ||
                 (ev.What == EventConstants.evCommand && ev.Message.Command == CommandConstants.cmCancel))
        {
            EndModal(CommandConstants.cmCancel);
            ClearEvent(ref ev);
        }
        else
        {
            base.HandleEvent(ref ev);
        }
    }

    public int HistoryWidth()
    {
        int width = 0;
        int count = THistoryList.HistoryCount(HistoryId);
        for (int i = 0; i < count; i++)
        {
            var str = THistoryList.HistoryStr(HistoryId, i);
            if (str != null)
            {
                width = Math.Max(width, str.Length);
            }
        }
        return width;
    }
}
