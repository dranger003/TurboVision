using System.Text.Json.Serialization;
using TurboVision.Core;

namespace TurboVision.Views;

/// <summary>
/// Scrollable content area.
/// </summary>
public class TScroller : TView
{
    private static readonly byte[] DefaultPalette = [0x06, 0x07];

    public TPoint Delta { get; set; }
    public TPoint Limit { get; protected set; }

    /// <summary>
    /// Reference to horizontal scroll bar. Not serialized - exists in SubViews.
    /// </summary>
    [JsonIgnore]
    public TScrollBar? HScrollBar { get; set; }

    /// <summary>
    /// Reference to vertical scroll bar. Not serialized - exists in SubViews.
    /// </summary>
    [JsonIgnore]
    public TScrollBar? VScrollBar { get; set; }

    protected byte DrawLock { get; set; }
    protected bool DrawFlag { get; set; }

    /// <summary>
    /// Parameterless constructor for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    protected TScroller() : base()
    {
        Options |= OptionFlags.ofSelectable;
        EventMask |= EventConstants.evBroadcast;
    }

    public TScroller(TRect bounds, TScrollBar? hScrollBar, TScrollBar? vScrollBar) : base(bounds)
    {
        HScrollBar = hScrollBar;
        VScrollBar = vScrollBar;
        Options |= OptionFlags.ofSelectable;
        EventMask |= EventConstants.evBroadcast;
    }

    public override void ChangeBounds(TRect bounds)
    {
        SetBounds(bounds);
        DrawLock++;
        SetLimit(Limit.X, Limit.Y);
        DrawLock--;
        DrawFlag = false;
        DrawView();
    }

    public override TPalette? GetPalette()
    {
        return new TPalette(DefaultPalette);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == EventConstants.evBroadcast &&
            ev.Message.Command == CommandConstants.cmScrollBarChanged &&
            (ev.Message.InfoPtr == HScrollBar || ev.Message.InfoPtr == VScrollBar))
        {
            ScrollDraw();
        }
    }

    public void ScrollTo(int x, int y)
    {
        DrawLock++;
        HScrollBar?.SetValue(x);
        VScrollBar?.SetValue(y);
        DrawLock--;
        CheckDraw();
    }

    public void SetLimit(int x, int y)
    {
        Limit = new TPoint(x, y);
        DrawLock++;

        HScrollBar?.SetParams(
            HScrollBar.Value,
            0,
            Math.Max(0, x - Size.X),
            Math.Max(1, Size.X - 1),
            1
        );

        VScrollBar?.SetParams(
            VScrollBar.Value,
            0,
            Math.Max(0, y - Size.Y),
            Math.Max(1, Size.Y - 1),
            1
        );

        DrawLock--;
        CheckDraw();
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & (StateFlags.sfActive | StateFlags.sfSelected)) != 0)
        {
            ShowSBar(HScrollBar);
            ShowSBar(VScrollBar);
        }
    }

    public void CheckDraw()
    {
        if (DrawLock == 0 && DrawFlag)
        {
            DrawFlag = false;
            DrawView();
        }
    }

    /// <summary>
    /// Called when scrollbars change. Updates delta and redraws if position changed.
    /// </summary>
    public virtual void ScrollDraw()
    {
        var d = new TPoint(
            HScrollBar?.Value ?? 0,
            VScrollBar?.Value ?? 0
        );

        if (d.X != Delta.X || d.Y != Delta.Y)
        {
            // Adjust cursor position to compensate for scroll
            SetCursor(Cursor.X + Delta.X - d.X, Cursor.Y + Delta.Y - d.Y);
            Delta = d;

            if (DrawLock != 0)
            {
                DrawFlag = true;
            }
            else
            {
                DrawView();
            }
        }
    }

    private void ShowSBar(TScrollBar? sBar)
    {
        if (sBar != null)
        {
            if (GetState(StateFlags.sfActive | StateFlags.sfSelected))
            {
                sBar.Show();
            }
            else
            {
                sBar.Hide();
            }
        }
    }

    public override void ShutDown()
    {
        HScrollBar = null;
        VScrollBar = null;
        base.ShutDown();
    }
}
