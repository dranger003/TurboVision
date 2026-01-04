using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Input history dropdown button.
/// </summary>
public class THistory : TView
{
    private static readonly byte[] DefaultPalette = [0x16, 0x17];

    // Icon: "\xDE~\x19~\xDD" = { ▐, ~, ▼, ~, ▌ }
    // Unicode equivalents matching upstream tvtext1.cpp
    private const string Icon = "\u2590~\u25BC~\u258C";

    public TInputLine? Link { get; set; }
    public ushort HistoryId { get; set; }

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
        b.MoveCStr(0, Icon, GetColor(0x0102));
        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if ((ev.What == EventConstants.evMouseDown) ||
            (ev.What == EventConstants.evKeyDown &&
             TStringUtils.CtrlToArrow(ev.KeyDown.KeyCode) == KeyConstants.kbDown &&
             Link != null &&
             (Link.State & StateFlags.sfFocused) != 0))
        {
            // Record current input line content before showing history
            if (Link != null && !string.IsNullOrEmpty(Link.Data))
            {
                THistoryList.HistoryAdd(HistoryId, Link.Data);
            }

            // Show the history window
            ushort result = ShowHistoryWindow();

            if (result == CommandConstants.cmOK && Link != null)
            {
                Link.SelectAll(true);
                Link.DrawView();
            }
            ClearEvent(ref ev);
        }
        else if (ev.What == EventConstants.evBroadcast &&
                 ev.Message.Command == CommandConstants.cmRecordHistory &&
                 ev.Message.InfoPtr == Link)
        {
            RecordHistory(Link?.Data ?? "");
            ClearEvent(ref ev);
        }
    }

    protected virtual ushort ShowHistoryWindow()
    {
        if (Link == null || Owner == null)
        {
            return CommandConstants.cmCancel;
        }

        // Calculate position for the history dropdown
        var linkGlobal = Link.Owner?.MakeGlobal(Link.Origin) ?? Link.Origin;
        int w = Math.Max(Link.Size.X + Size.X + 1, 20);
        int h = Math.Min(THistoryList.HistoryCount(HistoryId) + 2, 8);
        if (h < 4) h = 4;

        var r = new TRect(linkGlobal.X, linkGlobal.Y + 1, linkGlobal.X + w, linkGlobal.Y + 1 + h);

        // Ensure the window doesn't go off screen
        TView? topView = Owner.TopView();
        if (topView?.Owner != null)
        {
            var desktop = topView.Owner;
            if (r.B.X > desktop.Size.X)
            {
                r.Move(desktop.Size.X - r.B.X, 0);
            }
            if (r.B.Y > desktop.Size.Y)
            {
                r.Move(0, -(h + Link.Size.Y));
            }
            if (r.A.X < 0)
            {
                r.Move(-r.A.X, 0);
            }
            if (r.A.Y < 0)
            {
                r.Move(0, -r.A.Y);
            }
        }

        var histWin = new THistoryWindow(r, HistoryId);

        // Find the desktop/application to execute the window
        TGroup? deskTop = FindDesktop();
        if (deskTop == null)
        {
            return CommandConstants.cmCancel;
        }

        // Execute the window modally
        ushort result = deskTop.ExecView(histWin);

        if (result == CommandConstants.cmOK && histWin.Viewer != null)
        {
            // Get the selection
            Span<char> selection = stackalloc char[256];
            histWin.GetSelection(selection);
            int len = selection.IndexOf('\0');
            if (len < 0) len = selection.Length;
            if (len > 0)
            {
                Link.Data = new string(selection[..len]);
            }
        }

        histWin.Dispose();
        return result;
    }

    private TGroup? FindDesktop()
    {
        // Walk up the owner chain to find the desktop
        TView? v = Owner;
        while (v != null)
        {
            if (v is TDeskTop desktop)
            {
                return desktop;
            }
            if (v.Owner == null && v is TGroup group)
            {
                return group;
            }
            v = v.Owner;
        }
        return null;
    }

    public virtual void RecordHistory(string s)
    {
        if (!string.IsNullOrEmpty(s))
        {
            THistoryList.HistoryAdd(HistoryId, s);
        }
    }

    public override void ShutDown()
    {
        Link = null;
        base.ShutDown();
    }
}
