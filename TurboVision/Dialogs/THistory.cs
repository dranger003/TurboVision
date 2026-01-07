using System.Text.Json.Serialization;
using TurboVision.Application;
using TurboVision.Core;
using TurboVision.Views;

namespace TurboVision.Dialogs;

/// <summary>
/// Input history dropdown button.
/// </summary>
public class THistory : TView
{
    /// <summary>
    /// Type name for streaming identification.
    /// </summary>
    public new const string TypeName = "THistory";

    /// <inheritdoc/>
    [JsonIgnore]
    public override string StreamableName => TypeName;

    private static readonly byte[] DefaultPalette = [0x16, 0x17];

    // Icon: "\xDE~\x19~\xDD" = { ▐, ~, ▼, ~, ▌ }
    // Unicode equivalents matching upstream tvtext1.cpp
    private const string Icon = "\u2590~\u25BC~\u258C";

    /// <summary>
    /// Reference to the linked input line. Not serialized directly - use LinkIndex.
    /// </summary>
    [JsonIgnore]
    public TInputLine? Link { get; set; }

    /// <summary>
    /// Index of the linked input line in the parent's SubViews array.
    /// Used for serialization; resolved after deserialization by ViewHierarchyRebuilder.
    /// </summary>
    [JsonPropertyName("linkIndex")]
    public int LinkIndex { get; set; } = -1;

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
            RecordHistory(Link?.Data ?? "");

            // Show the history window
            ushort result = ShowHistoryWindow();

            if (result == CommandConstants.cmOK && Link != null)
            {
                Link.SelectAll(true);
                Link.DrawView();
            }
            ClearEvent(ref ev);
        }
        else if (ev.What == EventConstants.evBroadcast)
        {
            // Handle cmReleasedFocus (when link loses focus) or cmRecordHistory
            if ((ev.Message.Command == CommandConstants.cmReleasedFocus &&
                 ReferenceEquals(ev.Message.InfoPtr, Link)) ||
                ev.Message.Command == CommandConstants.cmRecordHistory)
            {
                RecordHistory(Link?.Data ?? "");
                // Note: upstream doesn't clear event for broadcasts
            }
        }
    }

    protected virtual ushort ShowHistoryWindow()
    {
        if (Link == null || Owner == null)
        {
            return CommandConstants.cmCancel;
        }

        // Try to focus the link first (like upstream)
        if (!Link.Focus())
        {
            return CommandConstants.cmCancel;
        }

        // Calculate position for the history dropdown (matching upstream thistory.cpp)
        // Use link's bounds relative to its owner
        var r = Link.GetBounds();
        r = new TRect(r.A.X - 1, r.A.Y - 1, r.B.X + 1, r.B.Y + 7);

        // Clip to owner's extent
        var ownerExtent = Owner.GetExtent();
        r = r.Intersect(ownerExtent);
        r = new TRect(r.A.X, r.A.Y, r.B.X, r.B.Y - 1);

        var histWin = InitHistoryWindow(r);
        if (histWin == null)
        {
            return CommandConstants.cmCancel;
        }

        // Execute the window modally in the owner (like upstream)
        ushort result = Owner.ExecView(histWin);

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

    protected virtual THistoryWindow? InitHistoryWindow(TRect bounds)
    {
        var histWin = new THistoryWindow(bounds, HistoryId);
        if (Link != null)
        {
            histWin.HelpCtx = Link.HelpCtx;
        }
        return histWin;
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
